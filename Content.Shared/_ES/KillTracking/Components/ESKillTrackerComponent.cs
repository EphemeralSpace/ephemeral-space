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
    public NetEntity? Source;

    [DataField]
    public FixedPoint2 AccumulatedDamage = FixedPoint2.Zero;

    public bool IsEnvironment => !Source.HasValue;
}

public static class ESKillTrackerHelpers
{
    public static ESDamageSource CreateKillTrackingSource(this IEntityManager entMan, FixedPoint2 accumulatedDamage)
    {
        return CreateKillTrackingSource(entMan, null, accumulatedDamage);
    }

    public static ESDamageSource CreateKillTrackingSource(this IEntityManager entMan, EntityUid? source, FixedPoint2 accumulatedDamage)
    {
        return new ESDamageSource
        {
            Source = entMan.GetNetEntity(source),
            AccumulatedDamage = accumulatedDamage,
        };
    }
}
