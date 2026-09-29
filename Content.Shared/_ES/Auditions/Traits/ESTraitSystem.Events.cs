using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Auditions.Traits;

public sealed partial class ESTraitSystem
{
    private void InitializeEvents()
    {
        SubscribeLocalEvent<MobStateComponent, ESAddComponentTraitEvent>(OnAddComponentTrait);
    }

    private void OnAddComponentTrait(Entity<MobStateComponent> ent, ref ESAddComponentTraitEvent args)
    {
        EntityManager.AddComponents(ent, args.Components);
    }
}

public sealed partial class ESAddComponentTraitEvent : ESTraitEvent
{
    [DataField(required: true)]
    public ComponentRegistry Components = new();
}
