using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Server.Administration;
using Content.Server.Chat;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.Masks.Nobleman;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Components;
using Content.Shared.Gibbing;
using Robust.Shared.Player;

namespace Content.Server._ES.Masks.Nobleman;

public sealed class ESTimedDemiseOnKillObjectiveSystem : ESBaseObjectiveSystem<ESTimedDemiseOnKillObjectiveComponent>
{
    [Dependency] private readonly ESEntityTimerSystem _timer = default!;
    [Dependency] private readonly SuicideSystem _suicide = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly ESSharedObjectiveSystem _objective = default!;

    public override Type[] RelayComponents => [typeof(ESKilledRelayComponent)];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESTimedDemiseOnKillObjectiveComponent, ESKilledPlayerEvent>(OnKilledPlayer);
        SubscribeLocalEvent<ESNoblemanKilledMarkerComponent, ESTimedDemiseOnKillEvent>(OnTimeToDie);
    }

    private void OnTimeToDie(Entity<ESNoblemanKilledMarkerComponent> ent, ref ESTimedDemiseOnKillEvent args)
    {
        if (!_suicide.Suicide(ent))
        {
            // you're not getting away that easily
            _gibbing.Gib(ent.Owner);
        }
    }

    private void OnKilledPlayer(Entity<ESTimedDemiseOnKillObjectiveComponent> ent, ref ESKilledPlayerEvent args)
    {
        if (args.Suicide)
            return;

        if (!MindSys.TryGetMind(args.Killed, out _))
            return;

        EnsureComp<ESNoblemanKilledMarkerComponent>(args.Killer);
        _timer.SpawnTimer(args.Killer, ent.Comp.TimeBeforeNoblemanDeath, new ESTimedDemiseOnKillEvent());

        if (!TryComp<ActorComponent>(args.Killer, out var actor))
            return;

        var title = Loc.GetString("es-mask-nobleman-killer-quickdialog-title");
        var msg = Loc.GetString("es-mask-nobleman-killer-quickdialog-msg");

        _quickDialog.OpenDialog<string>(actor.PlayerSession, title, msg, _ => {});
        ent.Comp.KilledAnyone = true;
        _objective.RefreshObjectiveProgress<ESTimedDemiseOnKillObjectiveComponent>();
    }

    protected override void GetObjectiveProgress(Entity<ESTimedDemiseOnKillObjectiveComponent> ent, ref ESGetObjectiveProgressEvent args)
    {
        if (!ent.Comp.KilledAnyone)
            args.Progress = ent.Comp.DefaultProgress;
        else
            args.Progress = 0;
    }
}
