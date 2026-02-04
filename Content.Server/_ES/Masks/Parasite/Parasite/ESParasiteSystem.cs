using Content.Server._ES.Masks.Objectives;
using Content.Server._ES.Masks.Objectives.Relays;
using Content.Server.Ghost;
using Content.Shared._ES.Masks;
using Content.Shared.Damage.Systems;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Parasite.Parasite;

public sealed class ESParasiteSystem : EntitySystem
{
    [Dependency] private readonly ESBeKilledObjectiveSystem _beKilled = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobstate = default!;
    [Dependency] private readonly ESSharedMaskSystem _mask = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESParasiteComponent, ESKillReportedEvent>(OnKillReported);
        SubscribeLocalEvent<ESParasiteComponent, GhostAttemptHandleEvent>(OnGhostAttempt);
    }

    private void OnKillReported(Entity<ESParasiteComponent> ent, ref ESKillReportedEvent args)
    {
        if (!_beKilled.IsValidKill(args, null, out var killerMind))
            return;

        ent.Comp.KillerMind = killerMind;

        _damageableSystem.ClearAllDamage(args.Entity);
        _mobstate.ChangeMobState(args.Entity, MobState.Alive);
    }

    private void OnGhostAttempt(Entity<ESParasiteComponent> ent, ref GhostAttemptHandleEvent args)
    {
        if (!TryComp<MindComponent>(ent, out var mindComp))
            return;

        if (mindComp.OwnedEntity is not { } ownedEntity || ent.Comp.KillerMind is not { } killerMind)
            return;

        if (killerMind.Value.Comp.OwnedEntity is not { } killerBody)
            return;

        if (!_mask.TryGetMask(killerBody, out var killermask))
            return;

        if (!_mask.TryGetMask(ownedEntity, out var victimMask))
            return;

        _mind.TransferTo(args.Mind, killerBody);
        _mind.TransferTo(killerMind, ownedEntity);

        _mask.ChangeMask(killerMind.AsNullable(), victimMask.Value);
        _mask.ChangeMask(args.Mind.AsNullable(), killermask.Value);

        args.Handled = true;
        args.Result = true;
    }
}
