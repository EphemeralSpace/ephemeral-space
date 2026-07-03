using Content.Server._ES.SecretIdentity.Objectives.Components;
using Content.Server._ES.SecretIdentity.Objectives.Relays;
using Content.Server._ES.SecretIdentity.Objectives.Relays.Components;
using Content.Shared._ES.Objectives;

namespace Content.Server._ES.SecretIdentity.Objectives;

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
