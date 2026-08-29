using Content.Shared._ES.Hazmat.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Shared._ES.Hazmat;

public abstract partial class ESSharedRemoveGasSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ESRemoveGasComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ESRemoveGasComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextClean = Timing.CurTime + TimeSpan.FromSeconds(1);
    }
}
