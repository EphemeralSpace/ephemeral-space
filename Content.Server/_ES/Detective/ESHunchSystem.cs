using System.Linq;
using Content.Server.Mind;
using Content.Shared._ES.Auditions;
using Content.Shared._ES.Detective;
using Content.Shared._ES.KillTracking;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;

namespace Content.Server._ES.Detective;

public sealed partial class ESHunchSystem : EntitySystem
{
    [Dependency] private ESCluesSystem _clue = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ESKillTrackingSystem _killTracking = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESHunchActionEvent>(OnHunch);
    }

    public void OnHunch(ESHunchActionEvent ev)
    {
        if (!_mobState.IsDead(ev.Target))
            return;

        var killer = _killTracking.GetKiller(ev.Target);

        if (killer == null)
        {
            _popup.PopupEntity(Loc.GetString("detective-hunch-fail"), ev.Performer, ev.Performer);
            ev.Handled = true;
            return;
        }

        if (!_mind.TryGetMind((EntityUid)killer, out var mind, out var comp))
            return;

        var hunch = EnsureComp<ESHunchComponent>(ev.Performer);

        if (hunch.BodyClue.TryGetValue(ev.Target, out var clue))
        {
            _popup.PopupEntity(Loc.GetString("detective-hunch", ("clue", clue)), ev.Performer, ev.Performer);
            ev.Handled = true;
            return;
        }

        var clues = _clue.GetClues(mind, 1);

        if (!clues.Any())
        {
            var emptyhunch = Loc.GetString("detective-hunch-empty");
            _popup.PopupEntity(emptyhunch, ev.Performer, ev.Performer);
            hunch.BodyClue.Add(ev.Target, emptyhunch);
            ev.Handled = true;
            return;
        }

        hunch.BodyClue.Add(ev.Target, clues.First());

        var msg = Loc.GetString("detective-hunch", ("clue", clues.First()));
        _popup.PopupEntity(msg, ev.Performer, ev.Performer);
        ev.Handled = true;
    }
}
