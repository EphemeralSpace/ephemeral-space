// ES START
// This completely overwrites the upstream thruster code. that's not great.
// Either port and merge these or break this out.
// ES END
using System.Diagnostics;
using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Audio;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Shared.Atmos;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Localizations;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Power;
using Content.Shared.Shuttles.Components;
using Content.Shared.Temperature;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Systems;

/// <summary>
/// Updates a thruster entity's thrust contribution to the ShuttleComponent that they are attached to.
/// </summary>
public sealed partial class ThrusterSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;
    [Dependency] private readonly FixtureSystem _fixtureSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;

    // TODO: Unhardcode this.
    public const string BurnFixture = "thruster-burn";
    private readonly Direction[] _cardinalDirections = [Direction.South, Direction.East, Direction.North, Direction.West];

    // Queries for frequently used components.
    private EntityQuery<ShuttleComponent> _shuttleQuery;
    private EntityQuery<TransformComponent> _thrusterTransformQuery;
    private EntityQuery<ThrusterComponent> _thrusterQuery;
    private EntityQuery<AppearanceComponent> _appearanceQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThrusterComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ThrusterComponent, ComponentInit>(OnThrusterInit);
        SubscribeLocalEvent<ThrusterComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<ThrusterComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<ThrusterComponent, ComponentShutdown>(OnThrusterShutdown);
        SubscribeLocalEvent<ThrusterComponent, PowerChangedEvent>(OnPowerChangedEvent);
        SubscribeLocalEvent<ThrusterComponent, AnchorStateChangedEvent>(OnAnchorChangedEvent);
        SubscribeLocalEvent<ThrusterComponent, ActivateInWorldEvent>(OnActivateInWorldEvent);
        SubscribeLocalEvent<ThrusterComponent, IsHotEvent>(OnIsHotEvent);
        SubscribeLocalEvent<ThrusterComponent, MoveEvent>(OnMoveEvent);
        SubscribeLocalEvent<ThrusterComponent, ExaminedEvent>(OnExaminedEvent);
        SubscribeLocalEvent<ThrusterComponent, AtmosDeviceUpdateEvent>(OnAtmosDeviceUpdateEvent);

        SubscribeLocalEvent<ShuttleComponent, TileChangedEvent>(OnShuttleTileChangedEvent);

        _shuttleQuery = GetEntityQuery<ShuttleComponent>();
        _thrusterTransformQuery = GetEntityQuery<TransformComponent>();
        _thrusterQuery = GetEntityQuery<ThrusterComponent>();
        _appearanceQuery = GetEntityQuery<AppearanceComponent>();
    }

    private void OnMapInit(Entity<ThrusterComponent> ent, ref MapInitEvent args)
    {
        // Set the next update time on the component to the current time
        // so things are properly synced.
        ent.Comp.NextFire = _timing.CurTime + ent.Comp.DamageCooldown;
    }

    private void OnThrusterInit(Entity<ThrusterComponent> ent, ref ComponentInit args)
    {
        _ambient.SetAmbience(ent, false);

        // It came into life wanting to be disabled, so keep it disabled.
        if (!ent.Comp.Enabled)
        {
            return;
        }

        if (CanThrusterEnable(ent))
        {
            TryEnableThruster(ent);
        }
    }

    /// <summary>
    /// RUns the shutdown logic when the component is being deleted.
    /// Ensures that we properly remove our thrust contributions and whatnot
    /// before we're removed.
    /// </summary>
    private void OnThrusterShutdown(Entity<ThrusterComponent> ent, ref ComponentShutdown args)
    {
        TryDisableThruster(ent);
    }

    /// <summary>
    /// Turns the thruster on or off depending on if it is allowed to.
    /// A power change might not allow the thruster to fire anymore,
    /// or it might be allowed to now.
    /// </summary>
    private void OnPowerChangedEvent(Entity<ThrusterComponent> ent, ref PowerChangedEvent args)
    {
        if (CanThrusterEnable(ent))
        {
            TryEnableThruster(ent);
        }
        else
        {
            TryDisableThruster(ent);
        }
    }

    /// <summary>
    /// Turns the thruster on or off depending on if it is allowed to.
    /// An anchor change might not allow the thruster to fire anymore,
    /// or it might be allowed to now.
    /// </summary>
    private void OnAnchorChangedEvent(Entity<ThrusterComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (CanThrusterEnable(ent))
        {
            TryEnableThruster(ent);
        }
        else
        {
            TryDisableThruster(ent);
        }
    }

    /// <summary>
    /// Toggles the Enabled status on the thruster and enables/disables it depending on that.
    /// </summary>
    private void OnActivateInWorldEvent(Entity<ThrusterComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
        {
            return;
        }

        ent.Comp.Enabled ^= true;

        if (CanThrusterEnable(ent))
        {
            TryEnableThruster(ent);
        }
        else
        {
            TryDisableThruster(ent);
        }
    }

    /// <summary>
    /// Returns if the thruster is hot or not.
    /// </summary>
    private void OnIsHotEvent(Entity<ThrusterComponent> ent, ref IsHotEvent args)
    {
        args.IsHot = ent.Comp.ThrusterType != ThrusterType.Angular && ent.Comp.IsOn;
    }

    /// <summary>
    /// Changes the thrust contributions of rotated thrusters to the right direction and tries to
    /// automatically enable/disable them unless disabled by UX.
    /// </summary>
    /// <param name="ent">Thruster that was rotated.</param>
    /// <param name="args">MoveEvent args.</param>
    private void OnMoveEvent(Entity<ThrusterComponent> ent, ref MoveEvent args)
    {
        // The thruster wasn't on, so it wasn't providing any impulse and thus doesn't need to be checked if
        // it can be turned on automatically.
        if (!ent.Comp.Enabled || !_thrusterTransformQuery.TryComp(ent, out var xform) ||
            !_shuttleQuery.TryComp(xform.GridUid, out var shuttleComp))
        {
            return;
        }

        var newEnt = new Entity<ThrusterComponent, TransformComponent?>(ent, ent.Comp, xform);

        var canEnable = CanThrusterEnable(newEnt);

        switch (canEnable)
        {
            // Don't enable the thruster inadvertently if the thruster wasn't on to begin with and we can't turn it on.
            case false when !newEnt.Comp1.IsOn:
                return;
            // Enable if the thruster was turned off but the new tile is valid.
            case true when !newEnt.Comp1.IsOn:
                TryEnableThruster(newEnt);
                return;
            // Disable if the new tile is invalid.
            case false when newEnt.Comp1.IsOn:
                TryDisableThruster(newEnt, args.OldRotation);
                break;
        }

        // Beyond this, the thruster has now rotated and stayed active.
        // We have to remove the thrust contribution from the old direction and add it
        // to the new direction.

        var oldDirection = args.OldRotation;
        var direction = args.NewRotation;
        var oldShuttleComp = shuttleComp;

        // Angular thrusters don't need to worry if their parent has changed.
        // Copy-paste but prettier.
        if (args.ParentChanged && ent.Comp.ThrusterType == ThrusterType.Angular)
        {
            oldShuttleComp = Comp<ShuttleComponent>(args.OldPosition.EntityId);

            // xform is resolved already
            ModifyThrustContribution(newEnt, oldShuttleComp, -newEnt.Comp1.Thrust, oldDirection);
            RemoveThrusterFromShuttleList(newEnt, oldShuttleComp);

            ModifyThrustContribution(newEnt, shuttleComp, newEnt.Comp1.Thrust, direction);
            AddThrusterToShuttleList(newEnt, shuttleComp);
            return;
        }

        if (ent.Comp.ThrusterType == ThrusterType.Linear)
        {
            // xform is resolved already
            ModifyThrustContribution(newEnt, oldShuttleComp, -newEnt.Comp1.Thrust, oldDirection);
            RemoveThrusterFromShuttleList(newEnt, oldShuttleComp);

            ModifyThrustContribution(newEnt, shuttleComp, newEnt.Comp1.Thrust, direction);
            AddThrusterToShuttleList(newEnt, shuttleComp);
        }
    }

    /// <summary>
    /// Pushes relevant examine text including on/off state and direction if it's a linear type thruster.
    /// </summary>
    /// <param name="ent">The thruster entity to push examine info to.</param>
    /// <param name="args">Args from ExaminedEvent.</param>
    private void OnExaminedEvent(Entity<ThrusterComponent> ent, ref ExaminedEvent args)
    {
        var enabled = Loc.GetString(ent.Comp.Enabled ? "thruster-comp-enabled" : "thruster-comp-disabled");

        using (args.PushGroup(nameof(ThrusterComponent)))
        {
            args.PushMarkup(enabled);

            if (ent.Comp.ThrusterType == ThrusterType.Linear &&
                _thrusterTransformQuery.TryComp(ent, out var xform) &&
                xform.Anchored)
            {
                var nozzleLocalization = ContentLocalizationManager
                    .FormatDirection(xform.LocalRotation.Opposite().ToWorldVec().GetDir())
                    .ToLower();
                var nozzleDir = Loc.GetString("thruster-comp-nozzle-direction",
                    ("direction", nozzleLocalization));

                args.PushMarkup(nozzleDir);

                var exposed = IsNozzleExposed((ent, ent.Comp, xform));

                var nozzleText =
                    Loc.GetString(exposed ? "thruster-comp-nozzle-exposed" : "thruster-comp-nozzle-not-exposed");

                args.PushMarkup(nozzleText);
            }
        }
    }

    /// <summary>
    /// Updates thruster performance and its ability to operate based on the inlet gas mixture.
    /// </summary>
    /// <param name="ent">The entity with the <see cref="ThrusterComponent"/> to update.</param>
    /// <param name="args">Args provided to us via AtmosDeviceUpdateEvent</param>
    private void OnAtmosDeviceUpdateEvent(Entity<ThrusterComponent> ent, ref AtmosDeviceUpdateEvent args)
    {
        if (!_nodeContainer.TryGetNode(ent.Owner, ent.Comp.InletName, out PipeNode? inlet))
        {
            return;
        }

        if (!_thrusterTransformQuery.TryComp(ent, out var xform))
        {
            return;
        }

        if (!_shuttleQuery.TryComp(xform.GridUid, out var shuttleComp))
        {
            return;
        }

        var newEnt = new Entity<ThrusterComponent, TransformComponent?>(ent.Owner, ent.Comp, xform);

        // First we need to compute our efficiency and thrust multiplier based on the gas mixture.
        // Define base thruster benefits/drawbacks.
        var finalEfficiency = 1f;
        var finalMultiplier = 1f;
        var isFueled = !newEnt.Comp1.RequiresFuel;

        // Run over our array
        // and build a final multiplicative fuel efficiency and thrust multiplier based on the gas mixture's effects.
        foreach (var mixture in newEnt.Comp1.GasMixturePair)
        {
            var benefit = 0f;

            // You'll never catch me writing this shit in upstream in a million years.
            switch (mixture.BenefitsCondition)
            {
                case GasMixtureBenefitsCondition.None:
                    if (AtmosphereSystem.HasAnyRequiredGas(mixture.Mixture, inlet.Air, Atmospherics.GasMinMoles))
                    {
                        benefit = 1f;
                        isFueled |= mixture.IsFuel;
                    }

                    break;

                case GasMixtureBenefitsCondition.SingleThreshold:
                    if (AtmosphereSystem.HasGasesAboveThreshold(mixture.Mixture, inlet.Air))
                    {
                        benefit = 1f;
                        isFueled |= mixture.IsFuel;
                    }

                    break;

                case GasMixtureBenefitsCondition.SingleThresholdPure:
                    benefit = AtmosphereSystem.GetPurityRatio(mixture.Mixture, inlet.Air);
                    if (benefit > 0f)
                    {
                        isFueled |= mixture.IsFuel;
                    }

                    break;

                // If the field is not present(???), fallback to Pure.
                case GasMixtureBenefitsCondition.Pure:
                    benefit = _atmosphere.GetGasMixtureSimilarity(mixture.Mixture, inlet.Air);
                    if (benefit > 0f)
                    {
                        isFueled |= mixture.IsFuel;
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(ent), "Invalid GasMixtureBenefitsCondition type.");
            }

            finalEfficiency *= benefit * mixture.ConsumptionEfficiency;
            finalMultiplier *= benefit * mixture.ThrustMultiplier;
        }

        newEnt.Comp1.GasConsumptionEfficiency = Math.Clamp(finalEfficiency,
            newEnt.Comp1.MinGasConsumptionEfficiency,
            newEnt.Comp1.MaxGasConsumptionEfficiency);
        newEnt.Comp1.GasThrustMultiplier = Math.Clamp(finalMultiplier,
            newEnt.Comp1.MinGasThrustMultiplier,
            newEnt.Comp1.MaxGasThrustMultiplier);

        var newThrust = newEnt.Comp1.GasThrustMultiplier * newEnt.Comp1.BaseThrust;
        var deltaThrust = newThrust - newEnt.Comp1.Thrust;

        newEnt.Comp1.Thrust = newThrust;
        newEnt.Comp1.HasFuel = isFueled;

        if (CanThrusterEnable(ent))
        {
            TryEnableThruster(ent);
        }
        else
        {
            TryDisableThruster(ent);
        }

        if (ent.Comp.IsOn)
        {
            ModifyThrustContribution(newEnt, shuttleComp, deltaThrust);
            RefreshShuttleCenterOfThrust(shuttleComp);
        }

        if (ent.Comp.Firing)
        {
            var gasConsumption = ent.Comp.BaseGasConsumptionRate * ent.Comp.GasConsumptionEfficiency;
            inlet.Air.Remove(gasConsumption);
        }
    }

    /// <summary>
    /// Disables thrusters whose thrust tile may have changed to no longer be a valid tile.
    /// </summary>
    /// <param name="ent">The shuttle entity.</param>
    /// <param name="args">Args from TileChangedEvent.</param>
    private void OnShuttleTileChangedEvent(Entity<ShuttleComponent> ent, ref TileChangedEvent args)
    {
        foreach (var change in args.Changes)
        {
            // The changed tile is still space.
            if (_turf.IsSpace(change.NewTile) || !_turf.IsSpace(change.OldTile))
                continue;

            var tilePos = change.GridIndices;
            var grid = Comp<MapGridComponent>(ent);

            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    if (x != 0 && y != 0)
                        continue;

                    var checkPos = tilePos + new Vector2i(x, y);
                    var enumerator = _mapSystem.GetAnchoredEntitiesEnumerator(ent, grid, checkPos);

                    while (enumerator.MoveNext(out var uid))
                    {
                        if (!_thrusterQuery.TryComp(uid.Value, out var thruster) || !thruster.RequireSpace)
                            continue;

                        // Work out if the thruster is facing this direction
                        var xform = _thrusterTransformQuery.GetComponent(uid.Value);
                        var direction = xform.LocalRotation.ToWorldVec();

                        if (new Vector2i((int)direction.X, (int)direction.Y) != new Vector2i(x, y))
                            continue;

                        TryDisableThruster((uid.Value, thruster, xform));
                    }
                }
            }
        }

    }

    /// <summary>
    /// Tries to disable the thruster. Does nothing if already disabled.
    /// </summary>
    /// <param name="ent">The thruster to disable.</param>
    /// <param name="angle">An optional angle to provide to remove the thrust from, useful if the thruster was rotated.</param>
    /// <returns>Whether the thruster was disabled or not, or if it was already disabled.</returns>
    public bool TryDisableThruster(Entity<ThrusterComponent, TransformComponent?> ent, Angle? angle = null)
    {
        if (!ent.Comp1.IsOn)
        {
            return false;
        }

        if (!_thrusterTransformQuery.Resolve(ent, ref ent.Comp2))
        {
            return false;
        }

        if (!_shuttleQuery.TryComp(ent.Comp2.GridUid, out var shuttleComp))
        {
            return false;
        }

        ModifyThrustContribution(ent, shuttleComp, -ent.Comp1.Thrust, angle);
        RemoveThrusterFromShuttleList(ent, shuttleComp);
        RefreshShuttleCenterOfThrust(shuttleComp);

        _fixtureSystem.DestroyFixture(ent, BurnFixture);
        ent.Comp1.Colliding.Clear();

        UpdateAppearance(ent.Owner, false);

        ent.Comp1.IsOn = false;

        return true;
    }

    /// <summary>
    /// Tries to enable the thruster and turn it on. Does nothing if already enabled.
    /// </summary>
    /// <param name="ent">The thruster to enable.</param>
    /// <returns>Whether the thruster was enabled or not, or if it was already enabled.</returns>
    /// <remarks>This method does not check for if a thruster is allowed to turn on before doing so,
    /// it simply just tries to turn it on. You should be checking the return of <see cref="CanThrusterEnable"/>
    /// if you want to ensure it's allowed to turn on before actually turning it on.</remarks>
    public bool TryEnableThruster(Entity<ThrusterComponent, TransformComponent?> ent)
    {
        // It's already on.
        if (ent.Comp1.IsOn)
        {
            return true;
        }

        if (!_thrusterTransformQuery.Resolve(ent, ref ent.Comp2) ||
            !_shuttleQuery.TryComp(ent.Comp2.GridUid, out var shuttleComp))
        {
            return false;
        }

        AddThrusterToShuttleList(ent, shuttleComp);
        ModifyThrustContribution(ent, shuttleComp, ent.Comp1.Thrust);
        TryAddThrusterBurnFixture(ent);
        RefreshShuttleCenterOfThrust(shuttleComp);

        UpdateAppearance(ent.Owner, true);

        ent.Comp1.IsOn = true;

        return true;
    }

    /// <summary>
    /// Updates the cosmetics (visuals, light, and ambiance) of the thruster given the provided value.
    /// </summary>
    /// <param name="uid">The thruster to update.</param>
    /// <param name="status">The state (true/false) to set on the thruster.</param>
    private void UpdateAppearance(EntityUid uid, bool status)
    {
        // Might as well use the cached comp. Think of the precious CPU cycles.
        if (_appearanceQuery.TryComp(uid, out var appearanceComp))
        {
            _appearance.SetData(uid, ThrusterVisualState.State, status, appearanceComp);
        }

        _light.SetEnabled(uid, status);
        _ambient.SetAmbience(uid, status);
    }

    /// <summary>
    /// Attempts to add the thruster burn fixture to the thruster.
    /// </summary>
    /// <param name="ent">The thruster to add the fixture to.</param>
    /// <returns>A true or false depending on whether the addition was successful.</returns>
    public bool TryAddThrusterBurnFixture(Entity<ThrusterComponent> ent)
    {
        if (ent.Comp.BurnPoly.Count <= 0 || ent.Comp.ThrusterType != ThrusterType.Linear) // Hardcoded just for you <3
            return false;

        var shape = new PolygonShape();
        shape.Set(ent.Comp.BurnPoly);
        return _fixtureSystem.TryCreateFixture(ent, shape, BurnFixture, hard: false, collisionLayer: (int)CollisionGroup.FullTileMask);
    }

    /// <summary>
    /// Removes a thruster from the list of thrusters contributing to a shuttle's impulse.
    /// </summary>
    /// <param name="ent">The thruster to remove from the list.</param>
    /// <param name="shuttleComp">The <see cref="ShuttleComponent"/> to remove the thruster from.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the thruster's type is
    /// out of range of the types available.</exception>
    public void RemoveThrusterFromShuttleList(Entity<ThrusterComponent, TransformComponent?> ent, ShuttleComponent shuttleComp)
    {
        _thrusterTransformQuery.Resolve(ent, ref ent.Comp2);
        Debug.Assert(ent.Comp2 != null);

        switch (ent.Comp1.ThrusterType)
        {
            case ThrusterType.Linear:
                var direction = (int)ent.Comp2.LocalRotation.GetCardinalDir() / 2;
                DebugTools.Assert(shuttleComp.LinearThrusters[direction].Contains(ent));
                shuttleComp.LinearThrusters[direction].Remove(ent);
                break;
            case ThrusterType.Angular:
                DebugTools.Assert(shuttleComp.AngularThrusters.Contains(ent));
                shuttleComp.AngularThrusters.Remove(ent);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(ent), "Invalid thruster type.");
        }
    }

    /// <summary>
    /// Adds a thruster to the list of thrusters contributing to a shuttle's impulse.
    /// </summary>
    /// <param name="ent">The thruster to add to the list.</param>
    /// <param name="shuttleComp">The <see cref="ShuttleComponent"/> to add the thruster to.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the thruster's type is
    /// out of range of the types available.</exception>
    public void AddThrusterToShuttleList(Entity<ThrusterComponent, TransformComponent?> ent, ShuttleComponent shuttleComp)
    {
        _thrusterTransformQuery.Resolve(ent, ref ent.Comp2);
        Debug.Assert(ent.Comp2 != null);

        switch (ent.Comp1.ThrusterType)
        {
            case ThrusterType.Linear:
                var direction = (int)ent.Comp2.LocalRotation.GetCardinalDir() / 2;
                DebugTools.Assert(!shuttleComp.LinearThrusters[direction].Contains(ent));
                shuttleComp.LinearThrusters[direction].Add(ent);
                break;
            case ThrusterType.Angular:
                DebugTools.Assert(!shuttleComp.AngularThrusters.Contains(ent));
                shuttleComp.AngularThrusters.Add(ent);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(ent), "Invalid thruster type.");
        }
    }

    /// <summary>
    /// Modify the thruster's impulse contribution to the given shuttle grid.
    /// </summary>
    /// <param name="ent">The thruster in question:</param>
    /// <param name="shuttleComp">The <see cref="ShuttleComponent"/> whose thrust directions to modify.</param>
    /// <param name="deltaThrust">The amount of thrust to add or subtract from the thruster's movement direction orientation.</param>
    /// <param name="angle">The angle (direction) to modify, used to override the current thrust direction contribution, useful when the thruster is rotated.</param>
    /// <remarks>This method does not automatically calculate the change in thrust from previous ticks and then applies this.
    /// It is an additive or subtractive application. Therefore, you <i>must</i> calculate thrust deltas yourself.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the thruster's type is
    /// out of range of the types available.</exception>
    public void ModifyThrustContribution(Entity<ThrusterComponent, TransformComponent?> ent, ShuttleComponent shuttleComp, float deltaThrust, Angle? angle = null)
    {
        _thrusterTransformQuery.Resolve(ent, ref ent.Comp2);
        Debug.Assert(ent.Comp2 != null);

        switch (ent.Comp1.ThrusterType)
        {
            // The thruster should already be a part of the list in this instance. If not, then we cry.
            case ThrusterType.Linear:
                angle ??= ent.Comp2.LocalRotation;
                var direction = (int)angle.Value.GetCardinalDir() / 2;
                DebugTools.Assert(shuttleComp.LinearThrusters[direction].Contains(ent));
                shuttleComp.LinearThrust[direction] += deltaThrust;
                break;
            case ThrusterType.Angular:
                DebugTools.Assert(shuttleComp.AngularThrusters.Contains(ent));
                shuttleComp.AngularThrust += deltaThrust;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(ent), "Invalid thruster type.");
        }
    }

    /// <summary>
    /// Refreshes a shuttle's center of thrust for movement calculations.
    /// </summary>
    private void RefreshShuttleCenterOfThrust(ShuttleComponent shuttleComp)
    {
        // TODO: Only refresh relevant directions.
        var center = Vector2.Zero;
        foreach (var dir in _cardinalDirections)
        {
            var index = (int)dir / 2;
            var pop = shuttleComp.LinearThrusters[index];
            var totalThrust = 0f;

            foreach (var thrusterUid in pop)
            {
                if (!_thrusterQuery.TryComp(thrusterUid, out var thrusterComp) ||
                    !_thrusterTransformQuery.TryComp(thrusterUid, out var xform))
                    continue;

                center += xform.LocalPosition * thrusterComp.Thrust;
                totalThrust += thrusterComp.Thrust;
            }

            center /= pop.Count * totalThrust;
            shuttleComp.CenterOfThrust[index] = center;
        }
    }

    /// <summary>
    /// Determines whether a thruster is capable of entering its idle (ready to fire) state.
    /// </summary>
    /// <returns>A true or false determining whether the thruster meets the conditions to provide impulse.</returns>
    public bool CanThrusterEnable(Entity<ThrusterComponent, TransformComponent?> ent)
    {
        // Someone has explicitly disabled their thruster through UX.
        if (!ent.Comp1.Enabled)
        {
            return false;
        }

        // Component is being thanos snapped right now.
        if (ent.Comp1.LifeStage > ComponentLifeStage.Running)
        {
            return false;
        }

        // Unanchored.
        if (_thrusterTransformQuery.Resolve(ent, ref ent.Comp2) && !ent.Comp2.Anchored)
        {
            return false;
        }

        // Needs power.
        if (ent.Comp1.RequirePower && !this.IsPowered(ent.Owner, EntityManager))
        {
            return false;
        }

        if (ent.Comp1 is { RequiresFuel: true, HasFuel: false })
        {
            return false;
        }

        if (ent.Comp1.RequireSpace)
        {
            return IsNozzleExposed(ent);
        }

        return true;
    }

    /// <summary>
    /// Determines if a thruster's exhaust is sitting on a valid tile.
    /// </summary>
    /// <param name="ent">Entity with optional <see cref="TransformComponent"/>.</param>
    private bool IsNozzleExposed(Entity<ThrusterComponent, TransformComponent?> ent)
    {
        return IsNozzleExposed((ent, ent.Comp2));
    }

    /// <summary>
    /// Determines if a thruster's exhaust is sitting on a valid tile.
    /// </summary>
    /// <param name="ent">Entity with optional <see cref="TransformComponent"/>.</param>
    private bool IsNozzleExposed(Entity<TransformComponent?> ent)
    {
        if (!_thrusterTransformQuery.Resolve(ent, ref ent.Comp))
        {
            /*
                    No TransformComponent?
              ⠀ ⣞⢽⢪⢣⢣⢣⢫⡺⡵⣝⡮⣗⢷⢽⢽⢽⣮⡷⡽⣜⣜⢮⢺⣜⢷⢽⢝⡽⣝
               ⠸⡸⠜⠕⠕⠁⢁⢇⢏⢽⢺⣪⡳⡝⣎⣏⢯⢞⡿⣟⣷⣳⢯⡷⣽⢽⢯⣳⣫⠇
               ⠀⠀⢀⢀⢄⢬⢪⡪⡎⣆⡈⠚⠜⠕⠇⠗⠝⢕⢯⢫⣞⣯⣿⣻⡽⣏⢗⣗⠏⠀
               ⠀⠪⡪⡪⣪⢪⢺⢸⢢⢓⢆⢤⢀⠀⠀⠀⠀⠈⢊⢞⡾⣿⡯⣏⢮⠷⠁⠀⠀
               ⠀⠀⠀⠈⠊⠆⡃⠕⢕⢇⢇⢇⢇⢇⢏⢎⢎⢆⢄⠀⢑⣽⣿⢝⠲⠉⠀⠀⠀⠀
               ⠀⠀⠀⠀⠀⡿⠂⠠⠀⡇⢇⠕⢈⣀⠀⠁⠡⠣⡣⡫⣂⣿⠯⢪⠰⠂⠀⠀⠀⠀
               ⠀⠀⠀⠀⡦⡙⡂⢀⢤⢣⠣⡈⣾⡃⠠⠄⠀⡄⢱⣌⣶⢏⢊⠂⠀⠀⠀⠀⠀⠀
               ⠀⠀⠀⠀⢝⡲⣜⡮⡏⢎⢌⢂⠙⠢⠐⢀⢘⢵⣽⣿⡿⠁⠁⠀⠀⠀⠀⠀⠀⠀
               ⠀⠀⠀⠀⠨⣺⡺⡕⡕⡱⡑⡆⡕⡅⡕⡜⡼⢽⡻⠏⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
               ⠀⠀⠀⠀⣼⣳⣫⣾⣵⣗⡵⡱⡡⢣⢑⢕⢜⢕⡝⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
               ⠀⠀⠀⣴⣿⣾⣿⣿⣿⡿⡽⡑⢌⠪⡢⡣⣣⡟⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
               ⠀⠀⠀⡟⡾⣿⢿⢿⢵⣽⣾⣼⣘⢸⢸⣞⡟⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
               ⠀⠀⠀⠀⠁⠇⠡⠩⡫⢿⣝⡻⡮⣒⢽⠋⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
             */
            return false;
        }

        if (ent.Comp.GridUid == null)
            return true;

        // Get the location of the tile directly behind the thruster relative to the world.
        var (x, y) = ent.Comp.LocalPosition + ent.Comp.LocalRotation.Opposite().ToWorldVec();

        // Get the actual tile at this location.
        var mapGrid = Comp<MapGridComponent>(ent.Comp.GridUid.Value);
        var tile = _mapSystem.GetTileRef(ent.Comp.GridUid.Value,
            mapGrid,
            new Vector2i((int)Math.Floor(x), (int)Math.Floor(y)));

        return _turf.IsSpace(tile);
    }

    /// <summary>
    /// Adds an entity who has started to collide with our <see cref="BurnFixture"/> to the colliding list.
    /// </summary>
    private void OnStartCollide(Entity<ThrusterComponent> ent, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != BurnFixture)
            return;

        ent.Comp.Colliding.Add(args.OtherEntity);
    }

    /// <summary>
    /// Removes an entity who has started to collide with our <see cref="BurnFixture"/> from the colliding list.
    /// </summary>
    private void OnEndCollide(Entity<ThrusterComponent> ent, ref EndCollideEvent args)
    {
        if (args.OurFixtureId != BurnFixture)
            return;

        ent.Comp.Colliding.Remove(args.OtherEntity);
    }

    /// <summary>
    /// Handles applying damage to entities currently in the colliding list.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<ThrusterComponent>();
        while (query.MoveNext(out var comp))
        {
            if (comp.NextFire > curTime)
                continue;

            comp.NextFire += comp.DamageCooldown;

            if (!comp.Firing || comp.Damage == null || comp.Colliding.Count == 0)
                continue;

            foreach (var uid in comp.Colliding)
            {
                _damageable.TryChangeDamage(uid, comp.Damage);
            }
        }
    }

    /// <summary>
    /// Sets an entire shuttle thruster direction to the firing state, updating its appearance.
    /// </summary>
    public void EnableLinearThrustDirection(ShuttleComponent shuttleComp, DirectionFlag direction)
    {
        if ((shuttleComp.ThrustDirections & direction) != 0x0)
        {
            return;
        }

        shuttleComp.ThrustDirections |= direction;

        var index = GetFlagIndex(direction);

        foreach (var uid in shuttleComp.LinearThrusters[index])
        {
            if (!_thrusterQuery.TryComp(uid, out var comp))
                continue;

            SetThrusterFiringState((uid, comp), true);
        }
    }

    /// <summary>
    /// Sets an entire shuttle thruster direction to the idle state, updating its appearance.
    /// </summary>
    public void DisableLinearThrustDirection(ShuttleComponent shuttleComp, DirectionFlag direction)
    {
        if ((shuttleComp.ThrustDirections & direction) == 0x0)
            return;

        shuttleComp.ThrustDirections &= ~direction;

        var index = GetFlagIndex(direction);

        foreach (var uid in shuttleComp.LinearThrusters[index])
        {
            if (!_thrusterQuery.TryComp(uid, out var comp))
                continue;

            SetThrusterFiringState((uid, comp), false);
        }
    }

    /// <summary>
    /// Disable all thrusters in all directions.
    /// </summary>
    public void DisableLinearThrusters(ShuttleComponent component)
    {
        foreach (var dir in Enum.GetValues<DirectionFlag>())
        {
            DisableLinearThrustDirection(component, dir);
        }

        DebugTools.Assert(component.ThrustDirections == DirectionFlag.None);
    }

    /// <summary>
    /// Sets the angular thrust on a ShuttleComponent.
    /// </summary>
    /// <param name="component"></param>
    /// <param name="on"></param>
    public void SetAngularThrust(ShuttleComponent component, bool on)
    {
        if (on)
        {
            foreach (var uid in component.AngularThrusters)
            {
                if (!_thrusterQuery.TryComp(uid, out var comp))
                    continue;

                SetThrusterFiringState((uid, comp), true);
            }
        }
        else
        {
            foreach (var uid in component.AngularThrusters)
            {
                if (!_thrusterQuery.TryComp(uid, out var comp))
                    continue;

                SetThrusterFiringState((uid, comp), false);
            }
        }
    }

    /// <summary>
    /// Sets the visual and firing state of a thruster.
    /// </summary>
    public void SetThrusterFiringState(Entity<ThrusterComponent, AppearanceComponent?> ent, bool state)
    {
        ent.Comp1.Firing = state;

        // Use our cached lookup for AppearanceComponent.
        // Making up for that update loop, okay?
        // TODO: Deviantart wolf pondering JPEG for if this needs to be logged
        _appearanceQuery.Resolve(ent, ref ent.Comp2, false);
        _appearance.SetData(ent, ThrusterVisualState.Thrusting, state, ent.Comp2);
    }

    /// <summary>
    /// Converts a given <see cref="DirectionFlag"/> to its corresponding zero-based index.
    /// </summary>
    /// <example>South = 0, East = 1, North = 2, West = 3</example>
    /// <param name="flag">The <see cref="DirectionFlag"/> to convert.</param>
    /// <returns>The zero-based index of the given flag, derived from its bit position.</returns>
    private static int GetFlagIndex(DirectionFlag flag)
    {
        // TODO: Is this something that already exists in engine Direction?
        return (int)Math.Log2((int)flag);
    }
}
