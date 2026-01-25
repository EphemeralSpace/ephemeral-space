using Content.Server._ES.Masks.Martyr.Components;
using Content.Server._ES.Masks.Objectives;
using Content.Server._ES.Masks.Objectives.Relays;
using Content.Server.Administration;
using Content.Server.Chat;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.Masks;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Shared._ES.Masks.Martyr;
using Content.Shared.Gibbing;

namespace Content.Server._ES.Masks.Martyr;

/// <summary>
///     Handles gameplay logic for the Martyr mask--i.e., checking if they were killed by a crewmember,
///     and marking their killer to be killed later as a result.
/// </summary>
/// <seealso cref="ESMartyrComponent"/>
/// <seealso cref="ESMartyrKillerMarkerComponent"/>
/// <seealso cref="ESBeKilledObjectiveSystem"/>
public sealed class ESMartyrSystem : EntitySystem
{
    [Dependency] private readonly SuicideSystem _suicide = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly ESEntityTimerSystem _timer = default!;
    [Dependency] private readonly ESBeKilledObjectiveSystem _beKilled = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;

    private static readonly ProtoId<ESTroupePrototype> KillerMustBeTroupe = "ESCrew";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESMartyrComponent, ESKillReportedEvent>(OnKillReported);
        SubscribeLocalEvent<ESMartyrKillerMarkerComponent, ESMartyrKillerTimeToDieEvent>(OnTimeToDie);
    }

    private void OnTimeToDie(Entity<ESMartyrKillerMarkerComponent> ent, ref ESMartyrKillerTimeToDieEvent args)
    {
        if (!_suicide.Suicide(ent))
        {
            // you're not getting away that easily
            _gibbing.Gib(ent.Owner);
        }
    }

    // we dont actually force this to be relayed --
    // instead we just assume that it will be relayed, if we are in the mind, because of our objective to be killed
    // if it isnt, then idk ur doing something wrong
    private void OnKillReported(Entity<ESMartyrComponent> ent, ref ESKillReportedEvent args)
    {
        if (!_beKilled.IsValidKill(args, KillerMustBeTroupe, out var killerMind))
            return;

        if (killerMind.Value.Comp.CurrentEntity is not { } killerBody)
            return;

        EnsureComp<ESMartyrKillerMarkerComponent>(killerBody);
        _ = _timer.SpawnTimer(killerBody, ent.Comp.TimeBeforeKillerDeath, new ESMartyrKillerTimeToDieEvent());

        if (!TryComp<ActorComponent>(killerBody, out var actor))
            return;

        var title = Loc.GetString("es-mask-martyr-killer-quickdialog-title");
        var msg = Loc.GetString("es-mask-martyr-killer-quickdialog-msg");

        // we are kind of misusing quickdialogs by just using them as a persistent UI popup rather than
        // entering any data, so we just ignore it with an empty action
        _quickDialog.OpenDialog<string>(actor.PlayerSession, title, msg, _ => {});
    }
}
