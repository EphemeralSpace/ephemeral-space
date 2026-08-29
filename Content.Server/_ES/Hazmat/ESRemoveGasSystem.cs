using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.EntitySystems;
using Content.Shared.Administration.Logs;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Piping.Components;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Atmos.Piping.Unary.Visuals;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Power;
using Content.Shared.Tools.Systems;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

using Content.Shared._ES.Hazmat.Components;
using Content.Shared._ES.Hazmat;

namespace Content.Server._ES.Hazmat;

public partial class ESRemoveGasSystem : ESSharedRemoveGasSystem
{
    [Dependency] private AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private TransformSystem _transformSystem = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = Timing.CurTime;

        var query = EntityQueryEnumerator<ESRemoveGasComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextClean > curTime)
                continue;

            var position = _transformSystem.GetGridTilePositionOrDefault(uid);
            var transform = CompOrNull<TransformComponent>(uid);
            if (transform == null)
            {
                Log.Debug("RemoveGas! Grid or transform component not found.");
                continue; // unsure how to handle error
            }
            var environment = _atmosphereSystem.GetTileMixture((uid, transform), true);
            if (environment != null)
            {
                Scrub(frameTime, comp, environment);

                comp.NextClean += comp.UpdateInterval;
            }
        }
    }

    // do final burst of cleaning
    private void Scrub(float timeDelta, ESRemoveGasComponent component, GasMixture tile)
    {
        // todo add transfer rate to component, and pick one.
        var transferRate = 5400 * _atmosphereSystem.PumpSpeedup();
        Log.Debug("RemoveGas! Removing gas at rate: " + transferRate);
        foreach (var gas in component.GasesToRemove)
        {
            var amountOfGas = tile.GetMoles(gas);
            var amountToReduceBy = timeDelta * transferRate;
            var adjustedAmountOfGas = MathF.Min(0f, amountOfGas - amountToReduceBy);
            Log.Debug("RemoveGas! Amount of gas left: " + adjustedAmountOfGas + ", original amount: " + amountOfGas);
            tile.AdjustMoles(gas, adjustedAmountOfGas);
        }
    }
}
