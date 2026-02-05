using Content.Server._ES.Masks.Objectives;
using Content.Server._ES.Masks.Objectives.Relays;
using Content.Server._ES.Masks.Parasite.Components;
using Content.Server.Ghost;
using Content.Shared._ES.Masks;
using Content.Shared.Damage.Systems;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;

namespace Content.Server._ES.Masks.Parasite;

public sealed class ESParasiteSystem : EntitySystem
{
    [Dependency] private readonly ESBeKilledObjectiveSystem _beKilledObjective = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ESSharedMaskSystem _mask = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESParasiteComponent, ESKillReportedEvent>(OnKillReported);
        SubscribeLocalEvent<ESParasiteComponent, GhostAttemptHandleEvent>(OnGhostAttempt);
    }

    private void OnKillReported(Entity<ESParasiteComponent> ent, ref ESKillReportedEvent args)
    {
        if (!_beKilledObjective.IsValidKill(args, null, out var killerMind))
            return;

        ent.Comp.KillerMind = killerMind;

        _damageable.ClearAllDamage(args.Entity);
        _mobState.ChangeMobState(args.Entity, MobState.Alive);
    }

    private void OnGhostAttempt(Entity<ESParasiteComponent> ent, ref GhostAttemptHandleEvent args)
    {
        if (!TryComp<MindComponent>(ent, out var mindComp))
            return;

        if (mindComp.OwnedEntity is not { } ownedEntity ||
            ent.Comp.KillerMind is not { } killerMind)
            return;

        if (!TryComp<MindComponent>(killerMind, out var killerMindComp))
            return;

        if (killerMindComp.OwnedEntity is not { } killerBody)
            return;

        if (!_mask.TryGetMask(killerBody, out var killerMask))
            return;

        if (!_mask.TryGetMask(ownedEntity, out var victimMask))
            return;

        _mind.TransferTo(args.Mind, killerBody);
        _mind.TransferTo(killerMind, ownedEntity);

        _mask.ChangeMask((killerMind, killerMindComp), victimMask.Value);
        _mask.ChangeMask(args.Mind, killerMask.Value);

        args.Handled = true;
        args.Result = true;
    }
}
