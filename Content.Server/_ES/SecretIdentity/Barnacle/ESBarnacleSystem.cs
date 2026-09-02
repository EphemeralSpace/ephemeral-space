using System.Numerics;
using System.Linq;
using Content.Server._ES.SecretIdentity.Hemophage.Components;
using Content.Server._ES.SecretIdentity.Parasite;
using Content.Server.Actions;
using Content.Server.Pinpointer;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._ES.Breakable;
using Content.Shared._ES.SecretIdentity;
using Content.Shared._ES.SecretIdentity.Barnacle;
using Content.Shared.Alert;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Localizations;
using Content.Shared.Maps;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;

namespace Content.Server._ES.SecretIdentity.Barnacle;

public sealed partial class ESBarnacleSystem : ESBaseParasiteSystem<ESBarnacleComponent>
{
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private GunSystem _gun = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private TurfSystem _turfSystem = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private TransformSystem _xform = default!;
    [Dependency] private ESSharedSecretIdentitySystem _secretIdentity = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESBarnacleActionEvent>(OnBarnacleAction);
        SubscribeLocalEvent<ESBarnacleDoAfterEvent>(OnBarnacleDoAfter);
        SubscribeLocalEvent<ESBarnacleComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ESBarnacleComponent, MindGotAddedEvent>(OnGotAdded);
        SubscribeLocalEvent<ESBarnacleComponent, MindGotRemovedEvent>(OnGotRemoved);
        SubscribeLocalEvent<ESBarnacleComponent, ESGetCharacterInfoBlurbEvent>(OnGetCharacterInfoBlurb);
        SubscribeLocalEvent<ESBarnacleMobComponent, ESBrokenStateChanged>(OnBarnacleDestroyed);
        SubscribeLocalEvent<ESBarnacleMobComponent, ESBarnacleDiedEvent>(OnBarnacleDied);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = AllEntityQuery<ESBarnacleProjectileComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var projectile, out var physics))
        {
            if (TerminatingOrDeleted(projectile.GoalEntity))
            {
                QueueDel(uid);
                continue;
            }

            var newVelocity = physics.LinearVelocity + Vector2.Normalize(physics.LinearVelocity) * projectile.AccelerationRate;
            _physics.SetLinearVelocity(uid, newVelocity);

            if (_transform.GetWorldPosition(uid).EqualsApprox(_transform.GetWorldPosition(projectile.GoalEntity), projectile.Tolerance) || _transform.GetGrid(uid) == null)
            {
                SpawnNextToOrDrop(projectile.BarnacleDead, uid);
                QueueDel(uid);
                continue;
            }

            var position = _transform.GetMoverCoordinates(uid);

            var onTile = false;
            foreach (var entity in _turfSystem.GetEntitiesInTile(position, LookupFlags.All))
            {
                if (!HasComp<ESSecretIdentityConvertOnCollideComponent>(entity))
                    continue;
                onTile = true; // This is a bit odd but its the only way I found that works, probably because Im stupid
                break;
            }

            if (!onTile)
                SpawnAtPosition(projectile.BarnacleSpawn, position.SnapToGrid());
        }
    }

    public void OnBarnacleDoAfter(ESBarnacleDoAfterEvent ev)
    {
        if (ev.Cancelled)
            return;

        var mob = SpawnAtPosition(ev.BarnacleEntityId, ev.TargetCoord.SnapToGrid(EntityManager));
        var comp = EnsureComp<ESBarnacleMobComponent>(mob);
        comp.BarnacleOwner = ev.Performer.Owner;

        ev.Performer.Comp2.Barnacles.Add(mob);
        UpdateAlert((ev.Performer, ev.Performer.Comp2, ev.Performer.Comp1));

        _actions.StartUseDelay(ev.Action);
        ev.Handled = true;
    }

    public void OnBarnacleAction(ESBarnacleActionEvent args)
    {
        if (!_mind.TryGetMind(args.Performer, out var mind))
            return;

        if (!TryComp<ESBarnacleComponent>(mind, out var barnacle))
            return;

        if (barnacle.Barnacles.Count >= barnacle.MaxBarnacles)
        {
            _popup.PopupEntity(Loc.GetString("barnacle-max"), args.Performer, args.Performer);
            return;
        }

        if (!_map.TryFindGridAt(_transform.ToMapCoordinates(args.Target), out var grid, out var gridCoord))
            return;

        var tileRef = _map.GetTileRef((grid, gridCoord), args.Target.SnapToGrid());

        if (_turfSystem.IsTileBlocked(tileRef, CollisionGroup.MobMask))
            return;

        var success = _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
                args.Performer,
                args.PlantDelay,
                new ESBarnacleDoAfterEvent
                {
                    BarnacleEntityId = args.BarnacleEntityId,
                    TargetCoord = args.Target,
                    Performer = (mind.Value.Owner, mind, barnacle),
                    Action = args.Action,
                },
                null)
        {
            Broadcast = true,
            BreakOnMove = true,
            BreakOnDamage = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        });

        if (success)
            _popup.PopupCoordinates(Loc.GetString("barnacle-planting-start"), args.Target.SnapToGrid());
    }

    protected override void OnValidParasiteKill(Entity<ESBarnacleComponent> ent, EntityUid killed, EntityUid killer, Entity<MindComponent> killedMind, Entity<MindComponent> killerMind)
    {
        foreach (var barnacle in ent.Comp.Barnacles)
        {
            var ev = new ESBarnacleDiedEvent();
            RaiseLocalEvent(barnacle, ref ev);
        }
    }

    private void OnStartup(Entity<ESBarnacleComponent> ent, ref ComponentStartup args)
    {
        UpdateAlert((ent, ent, null));
    }

    private void OnGotAdded(Entity<ESBarnacleComponent> ent, ref MindGotAddedEvent args)
    {
        UpdateAlert((ent, ent, null));
    }

    private void OnGotRemoved(Entity<ESBarnacleComponent> ent, ref MindGotRemovedEvent args)
    {
        _alerts.ClearAlert(args.Container.Owner, ent.Comp.BarnacleAlert);
    }

    private void OnBarnacleDestroyed(EntityUid uid, ESBarnacleMobComponent comp, ESBrokenStateChanged ev)
    {
        if (!ev.Broken)
            return;

        if (!TryComp<MindComponent>(comp.BarnacleOwner, out var mind) ||
            !TryComp<ESBarnacleComponent>(comp.BarnacleOwner, out var barnacle))
            return;

        barnacle.Barnacles.Remove(uid);

        if (mind.CurrentEntity is not { } owned)
            return;

        var msg = Loc.GetString("barnacle-destroyed", ("location", FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString(uid))));
        _popup.PopupEntity(msg, owned, owned, PopupType.MediumCaution);
        UpdateAlert((comp.BarnacleOwner, barnacle, mind));
    }

    private void OnBarnacleDied(EntityUid uid, ESBarnacleMobComponent comp , ESBarnacleDiedEvent ev)
    {
        if (!TryComp<MindComponent>(comp.BarnacleOwner, out var mind))
            return;

        if (mind.CurrentEntity is not { } owned)
            return;

        if (_transform.GetGrid(uid) != _transform.GetGrid(owned)) // Makes sure barnacle and killed are on same grid
            return;

        var direction = _transform.GetWorldPosition(owned) - _transform.GetWorldPosition(uid);

        var projectile = SpawnNextToOrDrop(comp.ProjectileId, uid);
        var projectileComp = EnsureComp<ESBarnacleProjectileComponent>(projectile);
        projectileComp.GoalEntity = owned;

        _gun.ShootProjectile(projectile, direction, Vector2.Zero, uid, uid, 0.01f);
    }

    private void UpdateAlert(Entity<ESBarnacleComponent?, MindComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return;

        if (ent.Comp2.CurrentEntity is not { } owned)
            return;

        var severity = _alerts.ClampSeverity(ent.Comp1.BarnacleAlert, (short) ent.Comp1.Barnacles.Count);
        _alerts.ShowAlert(owned, ent.Comp1.BarnacleAlert, severity);

        _secretIdentity.RefreshCharacterInfoBlurb((ent.Owner, ent.Comp2));
    }

    private void OnGetCharacterInfoBlurb(Entity<ESBarnacleComponent> ent, ref ESGetCharacterInfoBlurbEvent args)
    {
        if (ent.Comp.Barnacles.Count == 0)
            return;

        var locations = new List<string>();
        var directions = new List<string>();
        foreach (var barnacle in ent.Comp.Barnacles)
        {
            locations.Add(FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString(barnacle, true)));
            directions.Add(ContentLocalizationManager.FormatDirection((_xform.GetWorldPosition(barnacle) - _xform.GetWorldPosition(ent.Owner)).ToWorldAngle().GetDir()));
        }

        Console.Write(ContentLocalizationManager.FormatList(directions));
        args.Info.Add(FormattedMessage.FromMarkupPermissive(Loc.GetString("barnacle-location-character-info-blurb", ("location",  ContentLocalizationManager.FormatList(locations)), ("direction", ContentLocalizationManager.FormatList(directions)))));
    }

}
