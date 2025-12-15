using Content.Server._ES.Masks.Objectives.Components;
using Content.Server._ES.Masks.Objectives.Relays;
using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.Masks.Objectives;

public sealed class ESTakeDamageObjectiveSystem : ESBaseObjectiveSystem<ESTakeDamageObjectiveComponent>
{
    public override Type[] RelayComponents => new[] { typeof(ESDamageTakerRelayComponent) };

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESTakeDamageObjectiveComponent,  ESDamageTakenEvent>(OnDamageTaken);
    }

    protected override void InitializeObjective(Entity<ESTakeDamageObjectiveComponent> ent, ref ESInitializeObjectiveEvent args)
    {
        base.InitializeObjective(ent, ref args);

        if (!TryComp<ESCounterObjectiveComponent>(ent, out var counterObjective))
            return;

        ent.Comp.SelectedDamage = _proto.Index(_random.Pick(ent.Comp.RequiredDamages));

        _meta.SetEntityName(ent, Loc.GetString(ent.Comp.NameLoc, ("damagetype", ent.Comp.SelectedDamage.ID), ("count", counterObjective!.Target)));
        _meta.SetEntityDescription(ent, Loc.GetString(ent.Comp.DescriptionLoc, ("damagetype", ent.Comp.SelectedDamage.ID)));
    }

    private void OnDamageTaken(Entity<ESTakeDamageObjectiveComponent> ent, ref ESDamageTakenEvent args)
    {
        if (!args.DamageIncreased)
            return;

        if (!args.DamageDone.DamageDict.TryGetValue(ent.Comp.SelectedDamage!.ID, out var damage))
            return;

        if (damage <= 0)
            return;

        var currentDamage = (float)damage;

        ObjectivesSys.AdjustObjectiveCounter(ent.Owner, currentDamage);
    }
}
