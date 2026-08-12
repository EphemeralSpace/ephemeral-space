using Content.Server._ES.SecretIdentity.Nobleman.Components;
using Content.Server._ES.SecretIdentity.Objectives.Relays.Components;
using Content.Server._ES.SecretIdentity.Parasite.Components;
using Content.Server.Administration;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Robust.Shared.Player;

namespace Content.Server._ES.SecretIdentity.Parasite;

public sealed partial class ESParasiteDamageObjectiveSystem : ESBaseObjectiveSystem<ESParasiteDamageObjectiveComponent>
{
    [Dependency] private ESEntityTimerSystem _timer = default!;
    [Dependency] private QuickDialogSystem _quickDialog = default!;
    [Dependency] private MetaDataSystem _metadata = default!;

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

        var damageCap = ObjectivesSys.GetObjectiveCounterTarget(ent.Owner);
        var delta = (int) Math.Max(0, damageCap - damageDealt);
        _metadata.SetEntityDescription(ent.Owner, Loc.GetString("es-parasite-objective-do-no-harm-desc", ("damage", delta)));

        if (!ent.Comp.Failed && ObjectivesSys.GetProgress(ent.Owner) <= 0)
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
