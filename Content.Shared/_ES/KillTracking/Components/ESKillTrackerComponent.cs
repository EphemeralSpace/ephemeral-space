using Content.Shared.FixedPoint;

namespace Content.Shared._ES.KillTracking.Components;

[RegisterComponent]
public sealed partial class ESKillTrackerComponent : Component
{
    [DataField]
    public List<ESDamageSource> Sources = new();
}

[DataDefinition]
public sealed partial class ESDamageSource
{
    [DataField]
    public EntityUid? Source;

    [DataField]
    public FixedPoint2 AccumulatedDamage = FixedPoint2.Zero;

    public bool IsEnvironment => !Source.HasValue;

    public ESDamageSource(EntityUid? source, FixedPoint2 damage)
    {
        Source = source;
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

    public bool IsValidKill => !(IsSuicide || IsEnvironment);

    public bool IsSuicide => Killed == Killer;

    public bool IsEnvironment => !Killer.HasValue;
}
