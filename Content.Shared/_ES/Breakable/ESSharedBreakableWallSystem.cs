using Content.Shared._ES.Breakable.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._ES.Breakable;

public abstract partial class ESSharedBreakableWallSystem : EntitySystem
{
    [Dependency] private OccluderSystem _occluder = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESBreakableWallComponent, ESBrokenStateChanged>(OnBrokenStateChanged);
    }

    protected virtual void OnBrokenStateChanged(Entity<ESBreakableWallComponent> ent, ref ESBrokenStateChanged args)
    {
        _occluder.SetEnabled(ent.Owner, !args.Broken);
        _physics.SetCanCollide(ent.Owner, !args.Broken);
    }
}
