using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Content.IntegrationTests;
using Content.IntegrationTests.Pair;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Reactions;
using Content.Shared.Atmos;
using Robust.Shared;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Benchmarks;

/// <summary>
/// Benchmarks the performance of different gas reactions.
/// Tests each reaction type with realistic gas mixtures to measure computational cost.
/// </summary>
[Virtual]
[GcServer(true)]
[MemoryDiagnoser]
public class GasReactionBenchmark
{
    private const int Iterations = 1000;
    private TestPair _pair = default!;
    private AtmosphereSystem _atmosphereSystem = default!;

    // Grid and tile for reactions that need a holder
    private EntityUid _testGrid = default!;
    private TileAtmosphere _testTile = default!;
    // Reaction instances
    private PlasmaFireReaction _plasmaFireReaction = default!;
    private TritiumFireReaction _tritiumFireReaction = default!;
    private FrezonCoolantReaction _frezonCoolantReaction = default!;
    // Gas mixtures for each reaction type
    private GasMixture _plasmaFireMixture = default!;
    private GasMixture _tritiumFireMixture = default!;
    private GasMixture _frezonCoolantMixture = default!;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        ProgramShared.PathOffset = "../../../../";
        PoolManager.Startup();
        _pair = await PoolManager.GetServerClient(testContext: new ExternalTestContext("Benchmark", StreamWriter.Null));
        var server = _pair.Server;

        // Create test map and grid
        var mapData = await _pair.CreateTestMap();
        _testGrid = mapData.Grid;

        await server.WaitPost(() =>
        {
            var entMan = server.ResolveDependency<IEntityManager>();
            _atmosphereSystem = entMan.System<AtmosphereSystem>();

            _plasmaFireReaction = new PlasmaFireReaction();
            _tritiumFireReaction = new TritiumFireReaction();
            _frezonCoolantReaction = new FrezonCoolantReaction();

            SetupGasMixtures();
            SetupTile();
        });
    }

    private void SetupGasMixtures()
    {
        // Plasma Fire: Plasma + Oxygen at high temperature
        // Temperature must be > PlasmaMinimumBurnTemperature for reaction to occur
        _plasmaFireMixture = new GasMixture(Atmospherics.CellVolume)
        {
            Temperature = Atmospherics.PlasmaMinimumBurnTemperature + 100f // ~673K
        };
        _plasmaFireMixture.AdjustMoles(Gas.Plasma, 20f);
        _plasmaFireMixture.AdjustMoles(Gas.Oxygen, 100f);

        // Tritium Fire: Tritium + Oxygen at high temperature
        // Temperature must be > FireMinimumTemperatureToExist for reaction to occur
        _tritiumFireMixture = new GasMixture(Atmospherics.CellVolume)
        {
            Temperature = Atmospherics.FireMinimumTemperatureToExist + 100f // ~473K
        };
        _tritiumFireMixture.AdjustMoles(Gas.Tritium, 20f);
        _tritiumFireMixture.AdjustMoles(Gas.Oxygen, 100f);

        // Frezon Coolant: Frezon + Nitrogen
        // Temperature must be > FrezonCoolLowerTemperature (23.15K) for reaction to occur
        _frezonCoolantMixture = new GasMixture(Atmospherics.CellVolume)
        {
            Temperature = Atmospherics.T20C + 50f // ~343K
        };
        _frezonCoolantMixture.AdjustMoles(Gas.Frezon, 30f);
        _frezonCoolantMixture.AdjustMoles(Gas.Nitrogen, 100f);
    }

    private void SetupTile()
    {
        // Create a tile atmosphere to use as holder for all reactions
        var testIndices = new Vector2i(0, 0);
        _testTile = new TileAtmosphere(_testGrid, testIndices, new GasMixture(Atmospherics.CellVolume)
        {
            Temperature = Atmospherics.T20C
        });
    }

    private static GasMixture CloneMixture(GasMixture original)
    {
        return new GasMixture(original);
    }

    [Benchmark]
    public async Task PlasmaFireReaction()
    {
        await _pair.Server.WaitPost(() =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                var mixture = CloneMixture(_plasmaFireMixture);
                _plasmaFireReaction.React(mixture, _testTile, _atmosphereSystem, 1f);
            }
        });
    }

    [Benchmark]
    public async Task TritiumFireReaction()
    {
        await _pair.Server.WaitPost(() =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                var mixture = CloneMixture(_tritiumFireMixture);
                _tritiumFireReaction.React(mixture, _testTile, _atmosphereSystem, 1f);
            }
        });
    }

    [Benchmark]
    public async Task FrezonCoolantReaction()
    {
        await _pair.Server.WaitPost(() =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                var mixture = CloneMixture(_frezonCoolantMixture);
                _frezonCoolantReaction.React(mixture, _testTile, _atmosphereSystem, 1f);
            }
        });
    }

    [GlobalCleanup]
    public async Task CleanupAsync()
    {
        await _pair.DisposeAsync();
        PoolManager.Shutdown();
    }
}
