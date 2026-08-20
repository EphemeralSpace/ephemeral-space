using System.Numerics;
using Content.Client._ES.SoundOcclusion;
using Content.Shared._ES.Audio;
using Robust.Client;
using Robust.Client.Audio;
using Robust.Shared;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Client._ES.Audio;

/// <summary>
///     Overrides for clientside <see cref="AudioSystem"/> functions to use for our own Purposes
/// </summary>
public sealed partial class ESAudioOverrideSystem : EntitySystem
{
    [Dependency] private AudioSystem _originalAudio = default!;
    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private SharedTransformSystem _xformSys = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IBaseClient _baseClient = default!;
    [Dependency] private TomenoSoundOcclusionSystem _occlusion = default!;

    private ProtoId<AudioPresetPrototype> _reverbPreset = "Room";

    private const float OccludedSoundAmount = 1f;
    private const float OcclusionVolumeAdjust = -3f;
    private const float MinOcclusionPenetration = 0.8f;

    private const float MaxOcclusionFactor = 2.0f;
    private const float MaxOccludedDeltaDistance = 15.0f;
    private const float MinOccludedDeltaDistance = 0.5f;

    // ReSharper disable once InconsistentNaming
    // for vv testing purposes
    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<AudioPresetPrototype> ReverbPresetVV
    {
        get => _reverbPreset;
        set
        {
            if (_reverbPreset == value)
                return;

            _reverbPreset = value;
            _originalAudio.SetEffectPreset(_reverbEffect!.Value.Item1, _reverbEffect!.Value.Item2, _proto.Index(value));
            _originalAudio.SetEffect(_reverbAuxiliary!.Value.Item1, _reverbAuxiliary!.Value.Item2, _reverbEffect!.Value.Item1);
        }
    }

    private (EntityUid, AudioAuxiliaryComponent)? _reverbAuxiliary;
    private (EntityUid, AudioEffectComponent)? _reverbEffect;
    private float _maxRayLength;

    public override void Initialize()
    {
        base.Initialize();

        _originalAudio.ProcessStreamOverride += ProcessStream;
        _originalAudio.GetOcclusionOverride += GetOcclusion;
        _baseClient.RunLevelChanged += OnRunLevelChanged;

        //SubscribeLocalEvent<AudioComponent, ComponentShutdown>(OnAudioShutdown);

        Subs.CVar(_cfg, CVars.AudioRaycastLength, OnRaycastLengthChanged, true);
    }

    private void OnRunLevelChanged(object? sender, RunLevelChangedEventArgs e)
    {
        if (e.NewLevel == ClientRunLevel.InGame)
            InitializeAuxiliaryEffect();
    }

    private void InitializeAuxiliaryEffect()
    {
        _reverbAuxiliary = _originalAudio.CreateAuxiliary();
        _reverbEffect = _originalAudio.CreateEffect();
        _originalAudio.SetEffectPreset(_reverbEffect.Value.Item1, _reverbEffect.Value.Item2, _proto.Index(_reverbPreset));
        _originalAudio.SetEffect(_reverbAuxiliary.Value.Item1, _reverbAuxiliary.Value.Item2, _reverbEffect.Value.Item1);
    }

    private void OnRaycastLengthChanged(float value)
    {
        _maxRayLength = value;
    }

    private void ProcessStream(EntityUid entity, AudioComponent component, TransformComponent xform, MapCoordinates listener)
    {
        var wasStarted = component.Started;
        if (!component.Started)
        {
            component.Started = true;
            component.StartPlaying();
        }

        component.Velocity = Vector2.Zero;

        // If it's global but on another map (that isn't nullspace) then stop playing it.
        if (component.Global)
        {
            if (xform.MapID != MapId.Nullspace && listener.MapId != xform.MapID)
            {
                component.Gain = 0f;
                return;
            }

            // Resume playing.
            component.Volume = component.Params.Volume;
            return;
        }

        // Non-global sounds, stop playing if on another map.
        // Not relevant to us.
        if (listener.MapId != xform.MapID)
        {
            component.Gain = 0f;
            return;
        }

        var parentUid = xform.ParentUid;
        Vector2 worldPos;
        component.Volume = component.Params.Volume;

        // Handle grid audio differently by using grid position.
        if ((component.Flags & AudioFlags.GridAudio) != 0x0)
        {
            worldPos = _maps.GetGridPosition(parentUid);
        }
        else
        {
            worldPos = _xformSys.GetWorldPosition(entity);
        }

        // Max distance check
        var delta = worldPos - listener.Position;
        var distance = delta.Length();

        // Out of range so just clip it for us.
        if (_originalAudio.GetAudioDistance(distance) > component.MaxDistance)
        {
            // Still keeps the source playing, just with no volume.
            component.Gain = 0f;
            return;
        }

        if (_reverbAuxiliary is not null && !wasStarted)
        {
            (component as IAudioSource).SetAuxiliary(_reverbAuxiliary.Value.Item2.Auxiliary);
        }

        // Distance check
        if (distance > 0f && distance < 0.01f)
        {
            worldPos = listener.Position;
            delta = Vector2.Zero;
            distance = 0f;
        }

        // Update audio occlusion
        if ((component.Flags & AudioFlags.NoOcclusion) == AudioFlags.NoOcclusion)
        {
            component.Occlusion = 0f;
        }
        else
        {
            var occlusion = GetPathedEntityOcclusion(listener, delta, distance, entity, component);
            component.Occlusion = occlusion;
            if (component.Occlusion > 0f)
            {
                component.Volume = component.Params.Volume + (component.Occlusion * OcclusionVolumeAdjust);
            }
        }

        // Update audio positions.
        component.Position = worldPos;
    }

    /// <summary>
    /// Gets the audio occlusion from the target audio entity to the listener's position.
    /// </summary>
    public float GetPathedEntityOcclusion(MapCoordinates listener, Vector2 delta, float distance, EntityUid entity, AudioComponent component)
    {
        if (distance <= 0.1)
            return 0f;

        if (_occlusion.CurrentSoundPaths == null)
            return MaxOcclusionFactor;

        var listenerPos = _occlusion.CurrentSoundPaths.Stage.WorldToStage(listener.Position);
        var emitterPos = _occlusion.CurrentSoundPaths.Stage.WorldToStage(listener.Position + delta);

        var path = _occlusion.FindEntityPath(entity, emitterPos, component);

        return CalculatePathOcclusion(path, listenerPos, emitterPos);
    }

    private float CalculatePathOcclusion(TomenoSoundOcclusionSystem.PathResult? path,
        Vector2 listenerPos,
        Vector2 emitterPos)
    {
        if (path == null)
            return MaxOcclusionFactor;

        if (!path.ListenerPortal.HasValue && !path.EmitterPortal.HasValue)
            return 0f;

        var directDistance = (listenerPos - emitterPos).Length();

        var occludedDistance = path.PortalDistance;
        if (path.ListenerPortal.HasValue)
            occludedDistance += Vector2.Distance(listenerPos, path.ListenerPortal.Value);
        if (path.EmitterPortal.HasValue)
            occludedDistance += Vector2.Distance(emitterPos, path.EmitterPortal.Value);

        var distanceDelta = occludedDistance - directDistance;

        if (distanceDelta > MaxOccludedDeltaDistance)
            return MaxOcclusionFactor;
        if (distanceDelta < MinOccludedDeltaDistance)
            return 0f;

        return ((distanceDelta - MinOccludedDeltaDistance) / MaxOccludedDeltaDistance) * MaxOcclusionFactor;
    }

    public float GetOcclusion(MapCoordinates listener, Vector2 delta, float distance, EntityUid? entity)
    {
        if (distance <= 0.1)
            return 0f;

        if (_occlusion.CurrentSoundPaths == null)
            return MaxOcclusionFactor;

        var listenerPos = _occlusion.CurrentSoundPaths.Stage.WorldToStage(listener.Position);
        var emitterPos = _occlusion.CurrentSoundPaths.Stage.WorldToStage(listener.Position + delta);

        // TODO: this should be cached too... maybe we need the cache to be keyed by Object?
        var path = _occlusion.FindPath(emitterPos);

        return CalculatePathOcclusion(path, listenerPos, emitterPos);
    }
}
