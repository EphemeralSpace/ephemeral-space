using Content.IntegrationTests.Tests._Citadel.Attributes;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Gravity;
using Content.Shared.Atmos;
using Content.Shared.Gravity;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Citadel;

public abstract partial class GameTest
{
    [SidedDependency(Side.Server)] private readonly ITileDefinitionManager _serverTileDef = default!;
    [System(Side.Server)] private readonly MapSystem _serverMapSys = default!;
    [System(Side.Server)] private readonly GravitySystem _serverGravitySys = default!;
    [System(Side.Server)] private readonly AtmosphereSystem _serverAtmosphereSys = default!;

    private async Task FillTestMapArena()
    {
        const int size = 5;

        await Server.WaitPost(() =>
        {
            for (var x = -size; x <= size; x++)
            {
                for (var y = -size; y <= size; y++)
                {
                    SSetTile(TilePlating, new Vector2i(x, y), TestMap!.Grid);
                }
            }

            SAddGravity(TestMap!.Grid);
            SAddAtmosphere(TestMap!.MapUid, TestMap.Grid);
        });
    }

    /// <summary>
    /// Adds gravity to a given entity. Defaults to the grid if no entity is specified.
    /// </summary>
    public void SAddGravity(EntityUid target)
    {
        var gravity = SEntMan.EnsureComponent<GravityComponent>(target);
        _serverGravitySys.EnableGravity(target, gravity);
    }

    /// <summary>
    /// Adds a default atmosphere to the test map.
    /// </summary>
    public void SAddAtmosphere(EntityUid targetMap, EntityUid targetGrid)
    {
        var moles = new float[Atmospherics.AdjustedNumberOfGases];
        moles[(int)Gas.Oxygen] = 21.824779f;
        moles[(int)Gas.Nitrogen] = 82.10312f;
        _serverAtmosphereSys.SetMapAtmosphere(targetMap, false, new GasMixture(moles, Atmospherics.T20C));

        _serverAtmosphereSys.RebuildGridAtmosphere((targetGrid,
            SComp<GridAtmosphereComponent>(targetGrid),
            SComp<MapGridComponent>(targetGrid))
            );
    }

    /// <summary>
    /// Set the tile at the target position to some prototype.
    /// </summary>
    public void SSetTile(ProtoId<ContentTileDefinition>? proto, Vector2i coords, Entity<MapGridComponent> grid)
    {
        var tile = proto == null
            ? Tile.Empty
            : new Tile(_serverTileDef[proto].TileId);

        _serverMapSys.SetTile(grid, coords, tile);
    }

}
