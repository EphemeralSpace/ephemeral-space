using System.Linq;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Collections;

namespace Content.Shared._ES.KillTracking;

public sealed class ESKillTrackingSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESKillTrackerComponent, DamageChangedEvent>(OnDamageChanged, before: [ typeof(MobThresholdSystem) ]);
    }

    private void OnDamageChanged(Entity<ESKillTrackerComponent> ent, ref DamageChangedEvent args)
    {
        // I'm not really sure how we send a null delta.
        if (args.DamageDelta is not { } delta)
            return;

        ReduceDamage(ent.AsNullable(), DamageSpecifier.GetNegative(delta).GetTotal());
        AddDamage(ent.AsNullable(), args.Origin, DamageSpecifier.GetPositive(delta).GetTotal());
    }

    public void AddDamage(Entity<ESKillTrackerComponent?> ent, EntityUid? source, FixedPoint2 damage)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Sources.FirstOrDefault(e => GetEntity(e.Source) == source) is { } elem)
        {
            elem.AccumulatedDamage += damage;
        }
        else
        {
            ent.Comp.Sources.Add(EntityManager.CreateKillTrackingSource(source, damage));
        }
    }

    public void ReduceDamage(Entity<ESKillTrackerComponent?> ent, FixedPoint2 damage)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;
        var toRemove = new ValueList<ESDamageSource>();
        foreach (var source in ent.Comp.Sources)
        {
            source.AccumulatedDamage += damage;
            if (source.AccumulatedDamage <= 0)
                toRemove.Add(source);
        }

        foreach (var source in toRemove)
        {
            ent.Comp.Sources.Remove(source);
        }
    }
}
