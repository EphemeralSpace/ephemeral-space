using Content.Server.Administration.Logs;
using Content.Server.Destructible;
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

public sealed class MeteorSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
// ES START
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
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
