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

        SubscribeLocalEvent<ESHunchActionEvent>(OnHunch);
    }

    public void OnHunch(ESHunchActionEvent ev)
    {
        if (!_mobState.IsDead(ev.Target))
            return;
        ev.Handled = true;

        // Relies on non-networked data
        if (_net.IsClient)
            return;

        if (_killTracking.GetKiller(ev.Target) is not { } killer ||
            !_mind.TryGetMind(killer, out var mind))
        {
            _popup.PopupEntity(Loc.GetString("detective-hunch-fail", ("target", Identity.Entity(ev.Target, EntityManager))),
                ev.Performer,
                ev.Performer);
            return;
        }

        var comp = EnsureComp<ESHunchComponent>(ev.Performer);
        if (comp.BodyClue.TryGetValue(ev.Target, out var oldClue))
        {
            _popup.PopupEntity(oldClue,
                ev.Performer,
                ev.Performer);
            return;
        }

        if (_clue.GetClues(mind.Value.Owner, 1).FirstOrDefault() is not (var clueType, { } clue))
        {
            var emptyHunch = Loc.GetString("detective-hunch-empty", ("target", Identity.Entity(ev.Target, EntityManager)));
            _popup.PopupEntity(emptyHunch, ev.Performer, ev.Performer);
            comp.BodyClue.Add(ev.Target, emptyHunch);
            return;
        }

        var msg = Loc.GetString("detective-hunch",
            ("clue", clue),
            ("type", clueType),
            ("target", Identity.Entity(ev.Target, EntityManager)));
        comp.BodyClue.Add(ev.Target, msg);
        _popup.PopupEntity(msg, ev.Performer, ev.Performer);
    }
}
