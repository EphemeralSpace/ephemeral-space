using Content.Server._ES.Masks.Nobleman.Components;
using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Server._ES.Masks.Parasite.Components;
using Content.Server.Administration;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.Objectives;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Robust.Shared.Player;

namespace Content.Server._ES.Masks.Parasite;

public sealed partial class ESParasiteDamageObjectiveSystem : ESBaseObjectiveSystem<ESParasiteDamageObjectiveComponent>
{
    [Dependency] private ESEntityTimerSystem _timer = default!;
    [Dependency] private QuickDialogSystem _quickDialog = default!;

    public override Type[] RelayComponents { get; } = [typeof(ESDamageDealerRelayComponent)];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESParasiteDamageObjectiveComponent, ESCausedDamageChanged>(OnCausedDamageChanged);
    }

    private void OnCausedDamageChanged(Entity<ESParasiteDamageObjectiveComponent> ent, ref ESCausedDamageChanged args)
    {
        if (args.DamageDelta is null || !MindSys.TryGetMind(args.Entity, out _))
            return;

        // dont accumulate selfdmg
        if (args.Entity.Owner == args.Origin)
            return;

        var damageDealt = DamageSpecifier.GetPositive(args.DamageDelta).GetTotal();
        ObjectivesSys.AdjustObjectiveCounter(ent.Owner, damageDealt.Float());

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
