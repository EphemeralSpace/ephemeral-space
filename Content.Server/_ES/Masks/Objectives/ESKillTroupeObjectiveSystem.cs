using Content.Server._ES.Masks.Objectives.Components;
using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.Objectives;

namespace Content.Server._ES.Masks.Objectives;

/// <summary>
///     This handles the kill troupe objective.
/// </summary>
/// <seealso cref="ESKillTroupeObjectiveComponent"/>
public sealed class ESKillTroupeObjectiveSystem : ESBaseObjectiveSystem<ESKillTroupeObjectiveComponent>
{
    public override Type[] RelayComponents => [typeof(ESKilledRelayComponent)];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESKillTroupeObjectiveComponent, ESKilledPlayerEvent>(OnKill);
    }

    private void OnKill(Entity<ESKillTroupeObjectiveComponent> ent, ref ESKilledPlayerEvent args)
    {
        if (!args.ValidKill)
            return;

        if (!MaskSys.TryGetTroupe(args.Killed, out var troupe))
            return;

        if ((troupe == ent.Comp.Troupe) ^ ent.Comp.Invert)
            ObjectivesSys.AdjustObjectiveCounter(ent.Owner);
    }
}
