using Content.Server._ES.Masks.Objectives.Components;
using Content.Server._ES.Masks.Objectives.Relays;
using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Server.Administration.Logs;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Components;
using Content.Shared.Database;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.Masks.Objectives;

public sealed class ESTakeDamageFromSourceObjectiveSystem : ESBaseObjectiveSystem<ESTakeDamageFromSourceObjectiveComponent>
{
    public override Type[] RelayComponents => new[] { typeof(ESDamageTakerRelayComponent) };

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
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

        _meta.SetEntityDescription(ent, Loc.GetString(ent.Comp.DescriptionLoc, ("damagesource", ent.Comp.SelectedSource)));
    }

    private void OnDamageChanged(Entity<ESTakeDamageFromSourceObjectiveComponent> ent, ref ESDamageTakenEvent args)
    {
        if (!TryComp<TagComponent>(args.Origin, out var tag))
            return;

        if (_tag.HasTag(args.Body, ent.Comp.SelectedSource))
            return;

        if (!args.DamageIncreased)
            return;

        var totaldamage = args.DamageDone.GetTotal();

        ent.Comp.TotalDamage = (float)totaldamage + ent.Comp.TotalDamage;

        ObjectivesSys.SetObjectiveCounter(ent.Owner, ent.Comp.TotalDamage);
    }
}
