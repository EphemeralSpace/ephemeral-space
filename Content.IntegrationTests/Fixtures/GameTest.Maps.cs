using System.Diagnostics.CodeAnalysis;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Gravity;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Gravity;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Fixtures;

public abstract partial class GameTest
{
    [SidedDependency(Side.Server)] private readonly ITileDefinitionManager _serverTileDef = default!;
    [SidedDependency(Side.Server)] private readonly MapSystem _serverMapSys = default!;
    [SidedDependency(Side.Server)] private readonly GravitySystem _serverGravitySys = default!;
    [SidedDependency(Side.Server)] private readonly AtmosphereSystem _serverAtmosphereSys = default!;

    private async Task FillTestMapArena()
    {
        const int size = 5;

        await Server.WaitPost(() =>
        {
            for (var x = -size; x <= size; x++)
            {
                for (var y = -size; y <= size; y++)
                {
                    SSetTile("Plating", new Vector2i(x, y), TestMap!.Grid);
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

    /// <summary>
    ///     Fully loads a given map on the server, optionally initializing it, and runs the pair in sync for a few ticks
    ///     to ensure both sides have fully loaded the map.
    /// </summary>
    /// <remarks>
    ///     The test map is global to the game test and is exposed through the TestMap property when ready. Cleanup is
    ///     handled automatically as well.
    /// </remarks>
#pragma warning disable CS8774
    [MemberNotNull(nameof(TestMap))]
    public async Task<TestMapData> LoadTestMap(ResPath mapPath, bool initialized = true)
    {
        // C# is smart, but not that smart, we need to make a promise here.

        await Pair.LoadTestMap(mapPath, initialized);
        await RunUntilSynced();


        return TestMap!;
    }
#pragma warning restore

#pragma warning disable CS8774
    [MemberNotNull(nameof(TestMap))]
    public async Task CreateTestMap(TestMapMode kind, bool initialized = true)
    {
        if (TestMap is not null)
            throw new NotSupportedException("Dismantle your existing TestMap before creating a new one.");

        switch (kind)
        {
            case TestMapMode.None:
                break;
            case TestMapMode.Basic:
                await Pair.CreateTestMap(initialized);
                break;
            case TestMapMode.Arena:
                await Pair.CreateTestMap(initialized);
                await FillTestMapArena();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }

        await RunUntilSynced();

        // C# is smart, but not that smart, we need to make a promise here.
        // ReSharper disable once RedundantJumpStatement
        return;
    }
#pragma warning restore
}
