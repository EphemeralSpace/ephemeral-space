using Content.Server._ES.Masks.Objectives.Components;
using Content.Server._ES.Masks.Objectives.Relays;
using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Components;
using Content.Shared.Tag;
using Robust.Shared.Random;

namespace Content.Server._ES.Masks.Objectives;

public sealed class ESTakeDamageFromSourceObjectiveSystem : ESBaseObjectiveSystem<ESTakeDamageFromSourceObjectiveComponent>
{
    public override Type[] RelayComponents => new[] { typeof(ESDamageTakerRelayComponent) };

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESTakeDamageFromSourceObjectiveComponent, ESDamageTakenEvent>(OnDamageChanged);
    }

    protected override void InitializeObjective(Entity<ESTakeDamageFromSourceObjectiveComponent> ent, ref ESInitializeObjectiveEvent args)
    {
        base.InitializeObjective(ent, ref args);

        ent.Comp.SelectedSource = _random.Pick(ent.Comp.PossibleSources);

        _meta.SetEntityName(ent, Loc.GetString($"es-daredevil-source-objective-title-{ent.Comp.SelectedSource}", ("count", ObjectivesSys.GetObjectiveCounterTarget(ent.Owner))));
        _meta.SetEntityDescription(ent, Loc.GetString($"es-daredevil-source-objective-desc-{ent.Comp.SelectedSource}"));
    }

    private void OnDamageChanged(Entity<ESTakeDamageFromSourceObjectiveComponent> ent, ref ESDamageTakenEvent args)
    {
        if (!args.DamageIncreased)
            return;

        if (!_tag.HasTag(args.Body, ent.Comp.SelectedSource))
            return;

        ObjectivesSys.AdjustObjectiveCounter(ent.Owner, args.DamageDone.GetTotal().Float());
    }
}
