using System.Numerics;
using Content.Server._ES.SecretIdentity.Hemophage.Components;
using Content.Server._ES.SecretIdentity.Parasite;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._ES.Breakable;
using Content.Shared._ES.SecretIdentity.Barnacle;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Maps;
using Content.Shared.Mind;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server._ES.SecretIdentity.Barnacle;

public sealed partial class ESBarnacleSystem : ESBaseParasiteSystem<ESBarnacleComponent>
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private GunSystem _gun = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private TurfSystem _turfSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESBarnacleActionEvent>(OnBarnacleAction);
        SubscribeLocalEvent<ESBarnacleDoafterEvent>(OnBarnacleDoAfter);
        SubscribeLocalEvent<ESBarnacleMobComponent, ESBrokenStateChanged>(OnBarnacleDestroyed);
        SubscribeLocalEvent<ESBarnacleMobComponent, ESBarnacleDiedEvent>(OnBarnacleDied);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var ProjectileQuery = AllEntityQuery<ESBarnacleProjectileComponent>();

        while (ProjectileQuery.MoveNext(out var uid, out var projectile))
        {
            var physics = EnsureComp<PhysicsComponent>(uid);

            var NewVelocity = physics.LinearVelocity + Vector2.Normalize(physics.LinearVelocity) * projectile.AccelerationRate;
            _physics.SetLinearVelocity(uid, NewVelocity);

            if (_transform.GetWorldPosition(uid).EqualsApprox(_transform.GetWorldPosition(projectile.GoalVector), projectile.Tolerance) || _transform.GetGrid(uid) == null)
            {
                SpawnNextToOrDrop(projectile.BarnacleDead, uid);
                QueueDel(uid);
                return;
            }

            var Coord = _transform.GetMoverCoordinates(uid);

            var lookup = _turfSystem.GetEntitiesInTile(Coord, LookupFlags.All);

            var BarnacleOnTile = false;

            foreach (var entity in lookup)
            {
                if (HasComp<ESSecretIdentityConvertOnCollideComponent>(entity))
                {
                    BarnacleOnTile = true; // This is a bit odd but its the only way I found that works, probably because Im stupid
                    break;
                }
            }

            if (!BarnacleOnTile)
                SpawnAtPosition(projectile.BarnacleSpawn, Coord.SnapToGrid());
        }
    }

    public void OnBarnacleDoAfter(ESBarnacleDoafterEvent ev)
    {
        if (ev.Cancelled)
            return;



        var BarnacleMob = SpawnAtPosition("ESBarnacle", ev.TargetCoord.SnapToGrid(EntityManager));
        var Comp = EnsureComp<ESBarnacleMobComponent>(BarnacleMob);
        Comp.Owner = (ev.Preformer.Owner, ev.Preformer.Comp2, ev.Preformer.Comp1);

        ev.Preformer.Comp2.Barnacles.Add(BarnacleMob);

        ev.Handled = true;
    }

    public void OnBarnacleAction(ESBarnacleActionEvent action)
    {
        if (!_mind.TryGetMind(action.Performer, out var mind))
            return;

        if (!TryComp<ESBarnacleComponent>(mind, out var barnacle))
            return;

        if (barnacle.Barnacles.Count >= barnacle.MaxBarancle)
        {
            _popup.PopupEntity(Loc.GetString("barnacle-max"), action.Performer, action.Performer);
            return;
        }

        if (!_map.TryFindGridAt(_transform.ToMapCoordinates(action.Target), out var grid, out var gridCoord))
            return;

        var tileref = _map.GetTileRef((grid, gridCoord), action.Target.SnapToGrid());

        if (_turfSystem.IsTileBlocked(tileref, CollisionGroup.MobMask))
            return;

        action.Handled = _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, action.Performer, TimeSpan.FromSeconds(1), new ESBarnacleDoafterEvent{TargetCoord = action.Target, Preformer = (mind.Value.Owner, mind, barnacle)}, null)
        {Broadcast = true, BreakOnMove = true, BreakOnDamage = true,});
    }

    protected override void OnValidParasiteKill(Entity<ESBarnacleComponent> ent, EntityUid killed, EntityUid killer, Entity<MindComponent> killedMind, Entity<MindComponent> killerMind)
    {
        foreach (var barnacle in ent.Comp.Barnacles)
        {
            var ev = new ESBarnacleDiedEvent();
            RaiseLocalEvent(barnacle, ref ev);
        }
    }

    private void OnBarnacleDestroyed(EntityUid uid, ESBarnacleMobComponent comp, ESBrokenStateChanged ev)
    {
        if (ev.Broken)
        {
            comp.Owner.Comp1.Barnacles.Remove(uid);

            if (comp.Owner.Comp2.CurrentEntity is not { } owned)
                return;

            _popup.PopupEntity(Loc.GetString("barnacle-destroyed"), owned, owned, PopupType.MediumCaution);
        }
    }

    private void OnBarnacleDied(EntityUid uid, ESBarnacleMobComponent comp , ESBarnacleDiedEvent ev)
    {
        if (comp.Owner.Comp2.CurrentEntity is not { } owned)
            return;

        if (_transform.GetGrid(uid) != _transform.GetGrid(owned)) // Makes sure barnacle and killed are on same grid
            return;

        var Rotation = _transform.GetWorldPosition(owned) - _transform.GetWorldPosition(uid);

        var BarnacleProjectile = SpawnNextToOrDrop("ESProjectileBarnacle", uid);
        var Comp = EnsureComp<ESBarnacleProjectileComponent>(BarnacleProjectile);
        Comp.GoalVector = owned;

        _gun.ShootProjectile(BarnacleProjectile, Rotation, Vector2.Zero, uid, uid, 0.01f);

    }

}
