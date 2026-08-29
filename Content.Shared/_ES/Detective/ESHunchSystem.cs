using System.Linq;
using Content.Shared._ES.Auditions;
using Content.Shared._ES.Detective.Components;
using Content.Shared._ES.KillTracking;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Network;

namespace Content.Shared._ES.Detective;

public sealed partial class ESHunchSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private ESCluesSystem _clue = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ESKillTrackingSystem _killTracking = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESHunchComponent, ESHunchActionEvent>(OnHunch);
    }

    public void OnHunch(Entity<ESHunchComponent> ent, ref ESHunchActionEvent args)
    {
        if (!_mobState.IsDead(args.Target))
            return;
        args.Handled = true;

        // Relies on non-networked data
        if (_net.IsClient)
            return;

        if (_killTracking.GetKiller(args.Target) is not { } killer ||
            !_mind.TryGetMind(killer, out var mind))
        {
            _popup.PopupEntity(Loc.GetString("detective-hunch-fail", ("target", Identity.Entity(args.Target, EntityManager))),
                ent,
                ent);
            return;
        }

        if (ent.Comp.BodyClue.TryGetValue(args.Target, out var oldClue))
        {
            _popup.PopupEntity(oldClue,
                ent,
                ent);
            return;
        }

        if (_clue.GetClues(mind.Value.Owner, 1).FirstOrDefault() is not (var clueType, { } clue))
        {
            var emptyHunch = Loc.GetString("detective-hunch-empty", ("target", Identity.Entity(args.Target, EntityManager)));
            ent.Comp.BodyClue.Add(args.Target, emptyHunch);
            _popup.PopupEntity(emptyHunch, ent, ent);
            return;
        }

        var msg = Loc.GetString("detective-hunch",
            ("clue", clue),
            ("type", clueType),
            ("target", Identity.Entity(args.Target, EntityManager)));
        ent.Comp.BodyClue.Add(args.Target, msg);
        _popup.PopupEntity(msg, ent, ent);
    }
}
