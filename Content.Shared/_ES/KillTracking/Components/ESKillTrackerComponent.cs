using System.Diagnostics.CodeAnalysis;
using Content.Shared.FixedPoint;

namespace Content.Shared._ES.KillTracking.Components;

[RegisterComponent]
[Access(typeof(ESKillTrackingSystem))]
public sealed partial class ESKillTrackerComponent : Component
{
    [DataField]
    public List<ESDamageSource> Sources = new();
}

[DataDefinition]
public sealed partial class ESDamageSource
{
    [DataField]
    public EntityUid? Entity;

    [DataField]
    public FixedPoint2 AccumulatedDamage = FixedPoint2.Zero;

    public bool IsEnvironment => !Entity.HasValue;

    public ESDamageSource(EntityUid? entity, FixedPoint2 damage)
    {
        Entity = entity;
        AccumulatedDamage = damage;
    }
}

/// <summary>
/// Event raised on an entity with <see cref="ESKillTrackerComponent"/> when they die.
/// </summary>
[ByRefEvent]
public readonly struct ESPlayerKilledEvent(EntityUid killed, EntityUid? killer)
{
    public readonly EntityUid Killed = killed;

    public readonly EntityUid? Killer = killer;

    [MemberNotNullWhen(true, nameof(Killer))]
    public bool ValidKill => !(Suicide || Environment);

    [MemberNotNullWhen(true, nameof(Killer))]
    public bool Suicide => Killed == Killer;

    [MemberNotNullWhen(false, nameof(Killer))]
    public bool Environment => !Killer.HasValue;
}
