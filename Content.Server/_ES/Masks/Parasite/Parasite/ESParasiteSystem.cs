using Content.Server._ES.Masks.Martyr.Components;
using Content.Server._ES.Masks.Objectives;
using Content.Server._ES.Masks.Objectives.Relays;
using Content.Server.Administration;
using Content.Server.Chat;
using Content.Server.Construction.Completions;
using Content.Server.Ghost;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.Masks;
using Content.Shared._ES.Objectives.Target;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Parasite;

public sealed class ESParasiteSystem : EntitySystem
{
    [Dependency] private readonly SuicideSystem _suicide = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly ESEntityTimerSystem _timer = default!;
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
        if (!_beKilled.IsValidKill(args, null, out Entity<MindComponent>? killerMind))
            return;

        ent.Comp.KillerMind = killerMind;

        _damageableSystem.ClearAllDamage(args.Entity);
        _mobstate.ChangeMobState(args.Entity, MobState.Alive);
    }

    private void OnGhostAttempt(Entity<ESParasiteComponent> ent, ref GhostAttemptHandleEvent args)
    {
        if (!TryComp<MindComponent>(ent, out var mindComp))
            return;

        if (mindComp.OwnedEntity == null || ent.Comp.KillerMind == null)
            return;

        var OwnedEntity = mindComp.OwnedEntity;

        var KillerMind = ent.Comp.KillerMind;

        if (KillerMind.Value.Comp.OwnedEntity is not { } killerBody)
            return;

        if (!_mask.TryGetMask(killerBody, out var killermask))
            return;

        if (!_mask.TryGetMask((EntityUid)OwnedEntity, out var VictimMask))
            return;

        _mind.TransferTo((EntityUid)KillerMind, OwnedEntity);
        _mind.TransferTo(args.Mind, killerBody);


        _mask.ChangeMask((KillerMind.Value.Owner, KillerMind.Value.Comp), (ProtoId<ESMaskPrototype>)VictimMask);
        _mask.ChangeMask((args.Mind.Owner, args.Mind.Comp), (ProtoId<ESMaskPrototype>)killermask);

        args.Handled = true;
        args.Result = true;
    }
}
