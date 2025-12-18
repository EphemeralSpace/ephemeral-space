using Content.Server._ES.Masks.Objectives.Components;
using Content.Server._ES.Masks.Objectives.Relays;
using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Shared._ES.Objectives;

namespace Content.Server._ES.Masks.Objectives;

public sealed class ESTakeTotalDamageObjectiveSystem : ESBaseObjectiveSystem<ESTakeTotalDamageObjectiveComponent>
{
    public override Type[] RelayComponents => new[] { typeof(ESDamageTakerRelayComponent) };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESTakeTotalDamageObjectiveComponent,  ESDamageTakenEvent>(OnDamageTaken);
    }

    private void OnDamageTaken(Entity<ESTakeTotalDamageObjectiveComponent> ent, ref ESDamageTakenEvent args)
    {
        if (!args.DamageIncreased)
            return;

        ObjectivesSys.AdjustObjectiveCounter(ent.Owner, args.DamageDone.GetTotal().Float());
    }
}
