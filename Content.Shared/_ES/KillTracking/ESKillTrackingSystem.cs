using System.Linq;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Rejuvenate;
using Robust.Shared.Collections;

namespace Content.Shared._ES.KillTracking;

public sealed class ESKillTrackingSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESKillTrackerComponent, DamageChangedEvent>(OnDamageChanged, before: [ typeof(MobThresholdSystem) ]);
        SubscribeLocalEvent<ESKillTrackerComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<ESKillTrackerComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnDamageChanged(Entity<ESKillTrackerComponent> ent, ref DamageChangedEvent args)
    {
        // I'm not really sure how we send a null delta.
        if (args.DamageDelta is not { } delta)
            return;

        ReduceDamage(ent, DamageSpecifier.GetNegative(delta).GetTotal());
        AddDamage(ent, args.Origin, DamageSpecifier.GetPositive(delta).GetTotal());
    }

    private void OnRejuvenate(Entity<ESKillTrackerComponent> ent, ref RejuvenateEvent args)
    {
        ent.Comp.Sources.Clear();
    }

    private void OnMobStateChanged(Entity<ESKillTrackerComponent> ent, ref MobStateChangedEvent args)
    {
        // Only report on dead.
        if (args.NewMobState != MobState.Dead)
            return;

        var killer = ent.Comp.Sources.Count switch
        {
            > 1 => ent.Comp.Sources.Where(s => !s.IsEnvironment).MaxBy(s => s.AccumulatedDamage)?.Source,
            1 => ent.Comp.Sources.First().Source,
            _ => null,
        };

        // TODO: Testing logs.
        Log.Debug($"{ToPrettyString(ent)} was killed by {ToPrettyString(killer)}!");

        var ev = new ESPlayerKilledEvent(ent, killer);
        RaiseLocalEvent(ent, ref ev);
    }

    private void AddDamage(Entity<ESKillTrackerComponent> ent, EntityUid? source, FixedPoint2 damage)
    {
        if (ent.Comp.Sources.FirstOrDefault(e => e.Source == source) is { } elem)
        {
            elem.AccumulatedDamage += damage;
        }
        else
        {
            ent.Comp.Sources.Add(new ESDamageSource(source, damage));
        }

        if (source.HasValue)
        {
            // TODO: Relation tracking stuff goes here.
        }
    }

    private void ReduceDamage(Entity<ESKillTrackerComponent> ent, FixedPoint2 damage)
    {
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
