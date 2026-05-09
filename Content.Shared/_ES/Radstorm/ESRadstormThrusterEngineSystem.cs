using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.Radstorm.Components;
using Content.Shared._ES.Random;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Fluids;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Radstorm;

public sealed partial class ESRadstormThrusterEngineSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityLookupSystem _entityLookup = default!;
    [Dependency] private ESEntityTimerSystem _entityTimer = default!;
    [Dependency] private SharedPuddleSystem _puddle = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESRadstormThrusterEngineComponent, MapInitEvent>(OnMapInit, after: [ typeof(SharedSolutionContainerSystem) ]);
        SubscribeLocalEvent<ESRadstormThrusterEngineComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
        SubscribeLocalEvent<ESRadstormThrusterEngineComponent, ESThrusterEngineNoFuelTimerEvent>(OnThrusterEngineNoFuelTimer);
    }

    private void OnMapInit(Entity<ESRadstormThrusterEngineComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateRate;

        if (_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.FuelTankSolutionId, out var solution))
        {
            _solutionContainer.TryAddReagent(solution.Value,
                ent.Comp.FuelReagent,
                _random.Next(ent.Comp.MinStartingFuel, ent.Comp.MaxStartingFuel));
        }
    }

    private void OnThrusterEngineNoFuelTimer(Entity<ESRadstormThrusterEngineComponent> ent, ref ESThrusterEngineNoFuelTimerEvent args)
    {
        UpdateFuelState(ent);
    }

    private void OnSolutionChanged(Entity<ESRadstormThrusterEngineComponent> ent, ref SolutionContainerChangedEvent args)
    {
        if (args.SolutionId != ent.Comp.FuelTankSolutionId)
            return;

        if (!_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.FuelTankSolutionId, out var solution))
            return;

        if (args.Solution.GetReagentQuantity(new ReagentId(ent.Comp.FuelReagent, null)) != args.Solution.Volume)
        {
            var spillage = _solutionContainer.SplitSolutionWithout(solution.Value,
                args.Solution.Volume,
                ent.Comp.FuelReagent);
            _puddle.TrySpillAt(Transform(ent).Coordinates, spillage, out _);
        }

        var oldHasFuel = ent.Comp.HasFuel;

        ent.Comp.HasFuel = args.Solution.Volume > 0;

        // We've run out of fuel
        if (oldHasFuel && !ent.Comp.HasFuel)
        {
            _entityTimer.SpawnTimer(ent, ent.Comp.NoFuelDelay, new ESThrusterEngineNoFuelTimerEvent());
        }
        else if (ent.Comp.HasFuel && !oldHasFuel)
        {
            UpdateFuelState(ent);
        }
    }

    private void UpdateFuelState(Entity<ESRadstormThrusterEngineComponent> ent)
    {
        var coords = Transform(ent).Coordinates;
        var thrusters = _entityLookup.GetEntitiesInRange<ESRadstormModifierMachineComponent>(coords, 2.5f);

        if (thrusters.FirstOrNull() is not { } thruster)
            return;

        var ev = new ESThrusterEngineFuelStateChangedEvent(ent.Comp.HasFuel);
        RaiseLocalEvent(thruster, ref ev);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ESRadstormThrusterEngineComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.NextUpdate)
                continue;
            comp.NextUpdate += comp.UpdateRate;

            if (!_solutionContainer.TryGetSolution(uid, comp.FuelTankSolutionId, out var solution))
                continue;

            _solutionContainer.RemoveReagent(solution.Value, comp.FuelReagent, comp.FuelConsumptionRate);
        }
    }
}
