using Content.Server._ES.Masks.Psychid.Components;
using Content.Server.Ghost;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.Masks;
using Content.Shared.Administration.Systems;
using Content.Shared.Mind;

namespace Content.Server._ES.Masks.Psychid;

public sealed partial class ESPsychidSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private ESSharedMaskSystem _mask = default!;
    [Dependency] private RejuvenateSystem _rejuvenate = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESPsychidComponent, ESPlayerKilledEvent>(OnKillReported);
        SubscribeLocalEvent<ESPsychidComponent, GhostAttemptHandleEvent>(OnGhostAttempt);
    }

    private void OnKillReported(Entity<ESPsychidComponent> ent, ref ESPlayerKilledEvent args)
    {
        if (!args.ValidKill ||
            !_mind.TryGetMind(args.Killer.Value, out var killerMind) ||
            _mask.GetTroupeOrNull(killerMind.Value.AsNullable()) == ent.Comp.IgnoredTroupe)
        {
            ent.Comp.KillerMind = null;
            return;
        }

        ent.Comp.KillerMind = killerMind;

        // TODO ES with offmed this should really be doing something more interesting honestly
        _rejuvenate.PerformRejuvenate(args.Killed);
    }

    private void OnGhostAttempt(Entity<ESPsychidComponent> ent, ref GhostAttemptHandleEvent args)
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

        if (!_mask.TryGetMask(ownedEntity, out var victimMask))
            return;

        // ????
        _rejuvenate.PerformRejuvenate(ownedEntity);

        _mask.ChangeMask((killerMind, killerMindComp), victimMask.Value);
        _mind.SwapMinds(killerMind, killerBody, ent.Owner, ownedEntity);

        // Reset this so we don't hold a reference
        ent.Comp.KillerMind = null;

        args.Handled = true;
        args.Result = true;
    }
}
