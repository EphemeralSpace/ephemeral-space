using Content.Server._ES.Masks.Blessed.Components;
using Content.Server._ES.Masks.Objectives;
using Content.Server._ES.Masks.Objectives.Relays;
using Content.Server.Administration;
using Content.Server.Chat;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.Core.Timer.Components;
using Content.Shared._ES.Masks;
using Content.Shared.Body.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Blessed;

/// <summary>
///     Handles gameplay logic for the Blessed mask--i.e., checking if they were killed by a crewmember,
///     and marking their killer to be killed later as a result.
/// </summary>
/// <seealso cref="ESBlessedComponent"/>
/// <seealso cref="ESBlessedKillerMarkerComponent"/>
/// <seealso cref="ESBeKilledObjectiveSystem"/>
public sealed class ESBlessedSystem : EntitySystem
{
    [Dependency] private readonly SuicideSystem _suicide = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly ESEntityTimerSystem _timer = default!;
    [Dependency] private readonly ESBeKilledObjectiveSystem _beKilled = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;

    private static readonly ProtoId<ESTroupePrototype> KillerMustBeTroupe = "ESCrew";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESBlessedComponent, ESKillReportedEvent>(OnKillReported);
        SubscribeLocalEvent<ESBlessedKillerMarkerComponent, ESBlessedKillerTimeToDie>(OnTimeToDie);
    }

    private void OnTimeToDie(Entity<ESBlessedKillerMarkerComponent> ent, ref ESBlessedKillerTimeToDie args)
    {
        if (!_suicide.Suicide(ent))
        {
            // you're not getting away that easily
            _body.GibBody(ent.Owner);
        }
    }

    // we dont actually force this to be relayed --
    // instead we just assume that it will be relayed, if we are in the mind, because of our objective to be killed
    // if it isnt, then idk ur doing something wrong
    private void OnKillReported(Entity<ESBlessedComponent> ent, ref ESKillReportedEvent args)
    {
        if (!_beKilled.IsValidKill(args, KillerMustBeTroupe, out var killerMind))
            return;

        if (killerMind.Value.Comp.CurrentEntity is not { } killerBody)
            return;

        EnsureComp<ESBlessedKillerMarkerComponent>(killerBody);
        _timer.SpawnTimer(killerBody, ent.Comp.TimeBeforeKillerDeath, new ESBlessedKillerTimeToDie());

        if (!TryComp<ActorComponent>(killerBody, out var actor))
            return;

        var title = Loc.GetString("es-mask-blessed-killer-quickdialog-title");
        var msg = Loc.GetString("es-mask-blessed-killer-quickdialog-msg");

        // we are kind of misusing quickdialogs by just using them as a persistent UI popup rather than
        // entering any data, so we just ignore it with an empty action
        _quickDialog.OpenDialog<string>(actor.PlayerSession, title, msg, _ => {});
    }
}

/// <summary>
///     Raised directed after a time on the Blessed's killer when it's their time to die.
/// </summary>
public sealed partial class ESBlessedKillerTimeToDie : ESEntityTimerEvent;
