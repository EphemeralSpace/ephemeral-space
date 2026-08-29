using Content.Server._ES.SecretIdentity.Nobleman.Components;
using Content.Server._ES.SecretIdentity.Objectives.Relays.Components;
using Content.Server._ES.SecretIdentity.Parasite.Components;
using Content.Server.Administration;
using Content.Server.Popups;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server._ES.SecretIdentity.Parasite;

public sealed partial class ESParasiteDamageObjectiveSystem : ESBaseObjectiveSystem<ESParasiteDamageObjectiveComponent>
{
    [Dependency] private ESEntityTimerSystem _timer = default!;
    [Dependency] private QuickDialogSystem _quickDialog = default!;
    [Dependency] private MetaDataSystem _metadata = default!;
    [Dependency] private PopupSystem _popup = default!;

    public override Type[] RelayComponents { get; } = [typeof(ESDamageDealerRelayComponent)];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESParasiteDamageObjectiveComponent, ESCausedDamageChanged>(OnCausedDamageChanged);
    }

    protected override void InitializeObjective(Entity<ESParasiteDamageObjectiveComponent> ent, ref ESInitializeObjectiveEvent args)
    {
        base.InitializeObjective(ent, ref args);

        _metadata.SetEntityName(ent.Owner, Loc.GetString(ent.Comp.Title, ("damage", ObjectivesSys.GetObjectiveCounterTarget(ent.Owner))));
        ent.Comp.LastProgress = 1f;
    }

    private void OnCausedDamageChanged(Entity<ESParasiteDamageObjectiveComponent> ent, ref ESCausedDamageChanged args)
    {
        if (args.DamageDelta is null || !MindSys.TryGetMind(args.Entity, out _))
            return;

        // dont accumulate selfdmg
        if (args.Entity.Owner == args.Origin)
            return;

        var damageDealt = DamageSpecifier.GetPositive(args.DamageDelta).GetTotal().Float();
        ObjectivesSys.AdjustObjectiveCounter(ent.Owner, damageDealt);

        var progress = ObjectivesSys.GetProgress(ent.Owner);
        var delta = (int) Math.Max(0, progress * ObjectivesSys.GetObjectiveCounterTarget(ent.Owner));
        _metadata.SetEntityDescription(ent.Owner, Loc.GetString("es-parasite-objective-do-no-harm-desc", ("damage", delta)));

        var lastProgress = ent.Comp.LastProgress;
        var popupMsg = progress switch
        {
            <= 0.5f when lastProgress > 0.5f => "es-parasite-objective-do-no-harm-warning-50",
            <= 0.25f when lastProgress > 0.25f => "es-parasite-objective-do-no-harm-warning-75",
            <= 0.1f when lastProgress > 0.1f => "es-parasite-objective-do-no-harm-warning-90",
            _ => null
        };

        if (popupMsg != null)
            _popup.PopupEntity(Loc.GetString(popupMsg), args.Origin, args.Origin, PopupType.MediumCaution);

        ent.Comp.LastProgress = progress;

        if (!ent.Comp.Failed && progress <= 0)
        {
            ent.Comp.Failed = true;

            _timer.SpawnTimer(args.Origin, ent.Comp.KillDelay, new ESTimedDemiseOnKillEvent());

            if (!TryComp<ActorComponent>(args.Origin, out var actor))
                return;

            var title = Loc.GetString("es-parasite-killer-quickdialog-title");
            var msg = Loc.GetString("es-parasite-killer-quickdialog-msg");

            _quickDialog.OpenDialog<string>(actor.PlayerSession, title, msg, _ => {});
        }
    }
}
