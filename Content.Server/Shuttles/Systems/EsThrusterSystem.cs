using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Audio;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Shared.Damage;
using Content.Shared.Maps;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Systems;

/// <summary>
/// _ES specific refactored ThrusterSystem for managing thrusters, atmos I/O and shuttle movement contributions.
/// See <see cref="ThrusterSystem"/> for the original system.
/// </summary>
public sealed partial class EsThrusterSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;
    [Dependency] private readonly FixtureSystem _fixtureSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;

    // TODO: Unhardcode this.
    public const string BurnFixture = "thruster-burn";
    private readonly Direction[] _cardinalDirections = [Direction.South, Direction.East, Direction.North, Direction.West];

    // Queries for frequently used components.
    private EntityQuery<ShuttleComponent> _shuttleQuery;
    private EntityQuery<TransformComponent> _thrusterTransformQuery;
    private EntityQuery<EsThrusterComponent> _thrusterQuery;
    private EntityQuery<AppearanceComponent> _appearanceQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EsThrusterComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EsThrusterComponent, ComponentInit>(OnThrusterInit);
        SubscribeLocalEvent<EsThrusterComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<EsThrusterComponent, EndCollideEvent>(OnEndCollide);

        _shuttleQuery = GetEntityQuery<ShuttleComponent>();
        _thrusterTransformQuery = GetEntityQuery<TransformComponent>();
        _thrusterQuery = GetEntityQuery<EsThrusterComponent>();
    }

    private void OnMapInit(Entity<EsThrusterComponent> ent, ref MapInitEvent args)
    {
        // Set the next update time on the component to the current time
        // so things are properly synced.
        ent.Comp.NextFire = _timing.CurTime + ent.Comp.DamageCooldown;
    }

    private void OnThrusterInit(Entity<EsThrusterComponent> ent, ref ComponentInit args)
    {
        _ambient.SetAmbience(ent, false);

        // It came into life wanting to be disabled, so keep it disabled.
        if (!ent.Comp.Enabled)
        {
            return;
        }

        if (CanThrusterEnable(ent))
        {
            // TODO: Implement enabling method here. Goober.
        }
    }

    /// <summary>
    /// Tries to enable the thruster and turn it on. Does nothing if already enabled.
    /// </summary>
    /// <returns>Whether the thruster was enabled or not, or if it was already enabled.</returns>
    public bool TryEnableThruster(Entity<EsThrusterComponent, TransformComponent?> ent)
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

        // TODO: Finish rest lolxd
        return true;
    }

    /// <summary>
    /// Modify the thruster's impulse contribution to the given shuttle grid.
    /// </summary>
    /// <param name="ent">The thruster in question:</param>
    /// <param name="shuttleComp">The <see cref="ShuttleComponent"/> whose thrust directions to modify.</param>
    /// <param name="deltaThrust">The amount of thrust to add or subtract from the thruster's movement direction orientation.</param>
    /// <remarks>This method does not automatically calculate the change in thrust from previous ticks and then applies this.
    /// It is an additive or subtractive application. Therefore, you <i>must</i> calculate thrust deltas yourself.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the thruster's type is
    /// out of range of the types available to calculate contributions.</exception>
    public void ModifyThrustContribution(Entity<EsThrusterComponent, TransformComponent> ent, ShuttleComponent shuttleComp, float deltaThrust)
    {
        switch (ent.Comp1.Type)
        {
            // The thruster should already be a part of the list in this instance. If not, then we cry.
            case EsThrusterType.Linear:
                var direction = (int)ent.Comp2.LocalRotation.GetCardinalDir() / 2;
                DebugTools.Assert(shuttleComp.LinearThrusters[direction].Contains(ent));
                shuttleComp.LinearThrust[direction] += deltaThrust;
                break;

            case EsThrusterType.Angular:
                DebugTools.Assert(shuttleComp.AngularThrusters.Contains(ent));
                shuttleComp.AngularThrust += deltaThrust;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(deltaThrust), "Invalid thruster type.");
        }
    }

    /// <summary>
    /// Refreshes a shuttle's center of thrust for movement calculations.
    /// </summary>
    private void RefreshShuttleCenterOfThrust(Entity<ShuttleComponent> shuttleEnt)
    {
        // TODO: Only refresh relevant directions.
        var center = Vector2.Zero;
        foreach (var dir in _cardinalDirections)
        {
            var index = (int)dir / 2;
            var pop = shuttleEnt.Comp.LinearThrusters[index];
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
            shuttleEnt.Comp.CenterOfThrust[index] = center;
        }
    }

    /// <summary>
    /// Determines whether a thruster is capable of entering its idle (ready to fire) state.
    /// </summary>
    /// <returns>A true or false determining whether the thruster meets the conditions to provide impulse.</returns>
    public bool CanThrusterEnable(Entity<EsThrusterComponent, TransformComponent?> ent)
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

        if (ent.Comp1.RequireSpace)
        {
            return false;
        }

        // TODO: Fuel checking logic in here, or in atmos methods?
        return true;
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

        return tile.Tile.IsSpace();
    }

    /// <summary>
    /// Adds an entity who has started to collide with our <see cref="BurnFixture"/> to the colliding list.
    /// </summary>
    private void OnStartCollide(Entity<EsThrusterComponent> ent, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != BurnFixture)
            return;

        ent.Comp.Colliding.Add(args.OtherEntity);
    }

    /// <summary>
    /// Removes an entity who has started to collide with our <see cref="BurnFixture"/> from the colliding list.
    /// </summary>
    private void OnEndCollide(Entity<EsThrusterComponent> ent, ref EndCollideEvent args)
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

        var query = EntityQueryEnumerator<EsThrusterComponent>();
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
    public void SetThrusterFiringState(Entity<EsThrusterComponent, AppearanceComponent?> ent, bool state)
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
