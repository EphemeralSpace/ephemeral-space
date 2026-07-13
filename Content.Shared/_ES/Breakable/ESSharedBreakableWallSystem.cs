using Content.Shared._ES.Breakable.Components;
using Robust.Shared.Physics;
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

        if (TryComp<FixturesComponent>(ent, out var fixtures))
        {
            var layer = (int)(args.Broken ? ent.Comp.BrokenLayer : ent.Comp.BaseLayer);
            foreach (var (id, fixture) in fixtures.Fixtures)
            {
                if (!fixture.Hard)
                    continue;
                _physics.SetCollisionLayer(ent, id, fixture, layer, fixtures);
            }
        }
    }
}
