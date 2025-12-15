using Content.Server._ES.Masks.Objectives.Components;
using Content.Server._ES.Masks.Objectives.Relays;
using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

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

        var currentDamage = (float)args.DamageDone.GetTotal();

        ObjectivesSys.AdjustObjectiveCounter(ent.Owner, currentDamage);
    }
}
