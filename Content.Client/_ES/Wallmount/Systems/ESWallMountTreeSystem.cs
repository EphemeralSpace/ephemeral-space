using System.Numerics;
using Content.Client._ES.Wallmount.Components;
using Content.Shared.Wall;
using Robust.Client.GameObjects;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;

namespace Content.Client._ES.Wallmount.Systems;

/// <summary>
///     Handles updating the component tree for wallmount visibility purposes, so we can query it fast in <see cref="ESWallMountVisibilityOverlay"/>
/// </summary>
public sealed class ESWallMountTreeSystem : ComponentTreeSystem<ESWallMountTreeComponent, WallMountComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    protected override bool DoFrameUpdate => true;
    protected override bool DoTickUpdate => false;
    protected override bool Recursive => false;

    protected override Box2 ExtractAabb(in ComponentTreeEntry<WallMountComponent> entry, Vector2 pos, Angle rot)
    {
        // same as spritetree
        // if you dont have a spritecomp here you have problems
        return _sprite.CalculateBounds((entry.Uid, Comp<SpriteComponent>(entry.Uid)), pos, rot, default).CalcBoundingBox();
    }
}
