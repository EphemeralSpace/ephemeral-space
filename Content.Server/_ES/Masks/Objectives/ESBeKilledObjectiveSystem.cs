using System.Diagnostics.CodeAnalysis;
using Content.Server._ES.Masks.Objectives.Components;
using Content.Server._ES.Masks.Objectives.Relays;
using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Server.KillTracking;
using Content.Shared._ES.Masks;
using Content.Shared._ES.Objectives;
using Content.Shared.Mind;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Objectives;

/// <summary>
///     Handles objective logic for player-kills (e.g. for jester masks)
/// </summary>
public sealed class ESBeKilledObjectiveSystem : ESBaseObjectiveSystem<ESBeKilledObjectiveComponent>
{
    public override Type[] RelayComponents => [typeof(ESKilledRelayComponent)];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESBeKilledObjectiveComponent, ESKillReportedEvent>(OnKilled);
    }

    /// <summary>
    ///     Returns true if the given event corresponds to a valid kill on this entity, given the requirements.
    /// </summary>
    [PublicAPI]
    public bool IsValidKill(ESKillReportedEvent args, ProtoId<ESTroupePrototype>? requirement, [NotNullWhen(true)] out Entity<MindComponent>? killerMind)
    {
        killerMind = null;
        if (args.Suicide || args.Primary is not KillPlayerSource source)
            return false;

        if (!MindSys.TryGetMind(source.PlayerId, out var mind))
            return false;

        // troupe matches requirement?
        if (requirement != null &&
            MaskSys.GetTroupeOrNull(mind.Value.AsNullable()) is { } troupe
            && troupe != requirement)
            return false;

        killerMind = mind;
        return true;
    }

    private void OnKilled(Entity<ESBeKilledObjectiveComponent> ent, ref ESKillReportedEvent args)
    {
        if (!IsValidKill(args, ent.Comp.TroupeRequired, out _))
            return;

        ObjectivesSys.SetObjectiveCounter(ent.Owner, 1f);
    }
}
