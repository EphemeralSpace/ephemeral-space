using System.Numerics;
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

    private ProtoId<AudioPresetPrototype> _reverbPreset = "Room";

    private const float OccludedSoundAmount = 1f;
    private const float OcclusionVolumeAdjust = -6.5f;
    private const float MinOcclusionPenetration = 0.8f;

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
            var occlusion = GetOcclusion(listener, delta, distance, parentUid);
            component.Occlusion = occlusion;
            if (component.Occlusion > 0f)
            {
                component.Volume = component.Params.Volume + OcclusionVolumeAdjust;
                Log.Info($"occlusion set vol to {component.Volume}");
            }
        }

        // Update audio positions.
        component.Position = worldPos;
    }

    /// <summary>
    /// Gets the audio occlusion from the target audio entity to the listener's position.
    /// </summary>
    public float GetOcclusion(MapCoordinates listener, Vector2 delta, float distance, EntityUid? ignoredEnt = null)
    {
        if (distance <= 0.1)
            return 0f;

        var rayLength = MathF.Min(distance, _maxRayLength);
        var ray = new CollisionRay(listener.Position, delta / distance, _originalAudio.OcclusionCollisionMask);
        var penetration = _physics.IntersectRayPenetration(listener.MapId, ray, rayLength, ignoredEnt);

        return penetration < MinOcclusionPenetration ? 0f : OccludedSoundAmount;
    }
}
