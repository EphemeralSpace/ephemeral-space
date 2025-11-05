using Content.Shared._ES.Degradation.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Degradation;

/// <summary>
/// This handles equipment on the station slowly breaking and degrading over the course of the round.
/// Note that this happens in response to player events, not simply happening at will.
/// </summary>
public sealed class ESDegradationSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private static readonly EntProtoId SparkEffect = "EffectSparks";
    private static readonly  SoundSpecifier SparkSound = new SoundCollectionSpecifier("sparks");

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESQueuedDegradationComponent, DoorStateChangedEvent>(OnDoorStateChanged);
    }

    private void OnDoorStateChanged(Entity<ESQueuedDegradationComponent> ent, ref DoorStateChangedEvent args)
    {
        if (args.State != DoorState.Open)
            return;
        Degrade(ent.Owner, null);
    }

    public bool Degrade(EntityUid target, EntityUid? user)
    {
        var ev = new ESUndergoDegradationEvent(user);
        RaiseLocalEvent(target, ref ev);
        if (!ev.Handled)
            return false;

        // TODO: Proper Sparks
        _audio.PlayPredicted(SparkSound, target, user);
        PredictedSpawnAtPosition(SparkEffect, Transform(target).Coordinates);

        if (user.HasValue)
        {
            _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(target)} was degraded as a result of common interaction by {ToPrettyString(user):player}.");
        }
        else
        {
            _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(target)} was randomly degraded.");
        }

        // Remove if present
        RemCompDeferred<ESQueuedDegradationComponent>(target);
        return true;
    }
}

[ByRefEvent]
public record struct ESUndergoDegradationEvent(EntityUid? User, bool Handled = false);
