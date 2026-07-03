using System.Linq;
using Content.Server._ES.Objectives;
using Content.Server._ES.SecretIdentity.Parasite.Components;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.SecretIdentity;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.SecretIdentity.Parasite;

/// <summary>
///     Handles shared checks for parasite kill validity.
/// </summary>
public abstract partial class ESBaseParasiteSystem<T> : EntitySystem
    where T: Component
{
    [Dependency] protected SharedMindSystem Mind = default!;
    [Dependency] protected ESObjectiveSystem Objectives = default!;
    [Dependency] protected ESSecretIdentitySystem SecretIdentity = default!;

    private readonly ProtoId<ESOrganizationPrototype> _parasiteOrganization = "Parasite";

    public override void Initialize()
    {
        SubscribeLocalEvent<T, ESPlayerKilledEvent>(OnPlayerKilled);
    }

    private void OnPlayerKilled(Entity<T> ent, ref ESPlayerKilledEvent args)
    {
        if (!args.ValidKill || args.Killer is not { } killer)
            return;

        if (!TryComp<MindComponent>(ent.Owner, out var killedMind))
            return;

        if (!Mind.TryGetMind(killer, out var killerMindEntity, out var killerMind))
            return;

        if (Objectives.GetObjectives<ESParasiteDamageObjectiveComponent>(ent.Owner).Any(obj => obj.Comp.Failed))
            return;

        if (SecretIdentity.GetOrganizationOrNull((killerMindEntity, killerMind)) == _parasiteOrganization)
            return;

        OnValidParasiteKill(ent, args.Killed, args.Killer.Value, (ent.Owner, killedMind), (killerMindEntity, killerMind));
    }

    /// <summary>
    ///     Called when a valid parasite kill happens.
    ///     Parasite systems should perform their 'on-kill conversion' logic (or any other logic) in this function.
    /// </summary>
    protected abstract void OnValidParasiteKill(Entity<T> ent, EntityUid killed, EntityUid killer, Entity<MindComponent> killedMind, Entity<MindComponent> killerMind);
}
