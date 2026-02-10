using System.Linq;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._Offbrand.Wounds;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Rejuvenate;
using Robust.Shared.Collections;

namespace Content.Shared._ES.KillTracking;

public sealed class ESKillTrackingSystem : EntitySystem
{
    private const int SuicideSelfDamage = 200;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESKillTrackerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ESKillTrackerMarkerComponent, ComponentShutdown>(OnMarkerShutdown);

        SubscribeLocalEvent<ESKillTrackerComponent, DamageChangedEvent>(OnDamageChanged, before: [ typeof(MobThresholdSystem) ]);
        SubscribeLocalEvent<ESKillTrackerComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<ESKillTrackerComponent, SuicideEvent>(OnSuicide, before: [ typeof(BrainDamageSystem) ]);

        SubscribeLocalEvent<ESKillTrackerComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<ESKillTrackerComponent, DestructionEventArgs>(OnDestruction);
    }

    private void OnShutdown(Entity<ESKillTrackerComponent> ent, ref ComponentShutdown args)
    {
        foreach (var source in ent.Comp.Sources)
        {
            if (TryComp<ESKillTrackerMarkerComponent>(source.Entity, out var comp))
                comp.HurtEntities.Remove(ent);
        }
    }

    private void OnMarkerShutdown(Entity<ESKillTrackerMarkerComponent> ent, ref ComponentShutdown args)
    {
        foreach (var hurt in ent.Comp.HurtEntities)
        {
            if (TryComp<ESKillTrackerComponent>(hurt, out var comp))
                comp.Sources.RemoveAll(s => s.Entity == ent);
        }
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

    private void OnSuicide(Entity<ESKillTrackerComponent> ent, ref SuicideEvent args)
    {
        AddDamage(ent, ent, SuicideSelfDamage);
    }

    private void OnMobStateChanged(Entity<ESKillTrackerComponent> ent, ref MobStateChangedEvent args)
    {
        // Only report on dead.
        if (args.NewMobState != MobState.Dead)
            return;

        RaiseKillEvent(ent);
    }

    private void OnDestruction(Entity<ESKillTrackerComponent> ent, ref DestructionEventArgs args)
    {
        RaiseKillEvent(ent);
    }

    private void RaiseKillEvent(Entity<ESKillTrackerComponent> ent)
    {
        var killer = ent.Comp.Sources.Count switch
        {
            > 1 => ent.Comp.Sources.Where(s => !s.IsEnvironment).MaxBy(s => s.AccumulatedDamage)?.Entity,
            1 => ent.Comp.Sources.First().Entity,
            _ => null,
        };

        var ev = new ESPlayerKilledEvent(ent, killer);
        RaiseLocalEvent(ent, ref ev, true);
    }

    private void AddDamage(Entity<ESKillTrackerComponent> ent, EntityUid? source, FixedPoint2 damage)
    {
        if (source.HasValue && !HasComp<MobStateComponent>(source))
        {
            // Edge case: sometimes people are gonna pass stupid shit in for the origin
            // and we don't want inanimate objects counting as kills.
            return;
        }

        if (ent.Comp.Sources.FirstOrDefault(e => e.Entity == source) is { } elem)
        {
            elem.AccumulatedDamage += damage;
        }
        else
        {
            ent.Comp.Sources.Add(new ESDamageSource(source, damage));
        }

        if (source.HasValue)
        {
            var comp = EnsureComp<ESKillTrackerMarkerComponent>(source.Value);
            comp.HurtEntities.Add(ent);
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
