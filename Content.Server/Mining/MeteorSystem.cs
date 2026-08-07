using Content.Server.Administration.Logs;
using Content.Server.Destructible;
using Content.Shared._ES.Breakable;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
// ES START
using Content.Shared.Maps;
using Robust.Shared.Random;
// ES END

namespace Content.Server.Mining;

public sealed partial class MeteorSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private DestructibleSystem _destructible = default!;
    [Dependency] private MobThresholdSystem _mobThreshold = default!;
// ES START
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private TileSystem _tile = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private ESBreakableSystem _breakable = default!;
// ES END

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MeteorComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(EntityUid uid, MeteorComponent component, ref StartCollideEvent args)
    {
        if (TerminatingOrDeleted(args.OtherEntity) || TerminatingOrDeleted(uid))
            return;

        if (!args.OtherFixture.Hard)
            return;

        if (component.HitList.Contains(args.OtherEntity))
            return;

        FixedPoint2 threshold;
        if (_mobThreshold.TryGetDeadThreshold(args.OtherEntity, out var mobThreshold))
        {
            threshold = mobThreshold.Value;
            if (HasComp<ActorComponent>(args.OtherEntity))
                _adminLog.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(args.OtherEntity):player} was struck by meteor {ToPrettyString(uid):ent} and killed instantly.");
        }
        else if (_destructible.TryGetDestroyedAt(args.OtherEntity, out var destroyThreshold))
        {
            threshold = destroyThreshold.Value;
        }
        else if (_breakable.TryGetBrokenThreshold(args.OtherEntity, out var breakableThreshold))
        {
            threshold = breakableThreshold.Value;
        }
        else
        {
            threshold = FixedPoint2.MaxValue;
        }
        var otherEntDamage = CompOrNull<DamageableComponent>(args.OtherEntity)?.TotalDamage ?? FixedPoint2.Zero;
        // account for the damage that the other entity has already taken: don't overkill
        threshold -= otherEntDamage;

        // The max amount of damage our meteor can take before breaking.
        var maxMeteorDamage = _destructible.DestroyedAt(uid) - CompOrNull<DamageableComponent>(uid)?.TotalDamage ?? FixedPoint2.Zero;

        // Cap damage so we don't overkill the meteor
        var trueDamage = FixedPoint2.Min(maxMeteorDamage, threshold);

        var damage = component.DamageTypes * trueDamage;
        _damageable.TryChangeDamage(args.OtherEntity, damage, true, origin: uid);
        _damageable.TryChangeDamage(uid, damage);

        if (!TerminatingOrDeleted(args.OtherEntity))
            component.HitList.Add(args.OtherEntity);
    }
    // ES START
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MeteorComponent, TransformComponent>();
        while (query.MoveNext(out _, out var comp, out var xform))
        {
            if (!_turf.TryGetTileRef(xform.Coordinates, out var turfRef))
                continue;

            if (!_random.Prob(comp.TileBreakChance * frameTime))
                continue;

            _tile.DeconstructTile(turfRef.Value);
        }
    }
    // ES END
}
