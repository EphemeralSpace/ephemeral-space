using Content.Server._ES.SecretIdentity.Objectives.Components;
using Content.Server._ES.SecretIdentity.Objectives.Relays.Components;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.Objectives;

namespace Content.Server._ES.SecretIdentity.Objectives;

/// <summary>
///     This handles the kill organization objective.
/// </summary>
/// <seealso cref="ESKillOrganizationObjectiveComponent"/>
public sealed class ESKillOrganizationObjectiveSystem : ESBaseObjectiveSystem<ESKillOrganizationObjectiveComponent>
{
    public override Type[] RelayComponents => [typeof(ESKilledRelayComponent)];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESKillOrganizationObjectiveComponent, ESKilledPlayerEvent>(OnKill);
    }

    private void OnKill(Entity<ESKillOrganizationObjectiveComponent> ent, ref ESKilledPlayerEvent args)
    {
        if (!args.ValidKill)
            return;

        if (!SecretIdentitySys.TryGetOrganization(args.Killed, out var organization))
            return;

        if ((organization == ent.Comp.Organization) ^ ent.Comp.Invert)
            ObjectivesSys.AdjustObjectiveCounter(ent.Owner);
    }
}
