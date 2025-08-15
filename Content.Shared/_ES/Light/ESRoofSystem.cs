using System.Linq;
using Content.Shared._ES.Light.Components;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Light;

public sealed class ESRoofSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedRoofSystem _roof = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESTileBasedRoofComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ESTileBasedRoofComponent, TileChangedEvent>(OnTileChanged);
    }

    private void OnMapInit(Entity<ESTileBasedRoofComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<MapGridComponent>(ent, out var grid))
            return;

        RemComp<ImplicitRoofComponent>(ent);
        var roof = EnsureComp<RoofComponent>(ent);
        var tiles = ent.Comp.UnRoofedTiles.Select(p => (int) _prototype.Index(p).TileId).ToHashSet();

        // GOD we should batch these
        var enumerator = _map.GetAllTilesEnumerator(ent, grid, ignoreEmpty: true);
        while (enumerator.MoveNext(out var tile))
        {
            _roof.SetRoof((ent.Owner, grid, roof), tile.Value.GridIndices, !tiles.Contains(tile.Value.Tile.TypeId));
        }
    }

    private void OnTileChanged(Entity<ESTileBasedRoofComponent> ent, ref TileChangedEvent args)
    {
        if (!TryComp<MapGridComponent>(ent, out var grid))
            return;
        var roof = EnsureComp<RoofComponent>(ent);

        var tiles = ent.Comp.UnRoofedTiles.Select(p => (int) _prototype.Index(p).TileId).ToHashSet();

        foreach (var entry in args.Changes)
        {
            _roof.SetRoof((ent.Owner, grid, roof), entry.GridIndices, !tiles.Contains(entry.NewTile.TypeId));
        }
    }
}
