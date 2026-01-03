using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server._ES.StationEvents.Meteors.Components;

/// <summary>
/// Used for a station event that shoots a few waves of meteors at the station.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(ESMeteorsRule))]
public sealed partial class ESMeteorsRuleComponent : Component
{
    /// <summary>
    /// Angle the meteors are launched from
    /// </summary>
    [DataField]
    public Angle LaunchAngle;

    /// <summary>
    /// The meteors that are spawned each wave
    /// </summary>
    [DataField]
    public EntityTableSelector MeteorTable = new NoneSelector();

    /// <summary>
    /// How fast the meteors fly
    /// </summary>
    [DataField]
    public float MeteorVelocity = 10f;

    /// <summary>
    /// How many waves of meteors will spawn
    /// </summary>
    [DataField]
    public int Waves = 3;

    /// <summary>
    /// When the next wave will occur
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextWaveTime;

    /// <summary>
    /// Minimum delay between waves
    /// </summary>
    [DataField]
    public TimeSpan MinWaveDelay = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Maximum delay between waves
    /// </summary>
    [DataField]
    public TimeSpan MaxWaveDelay = TimeSpan.FromSeconds(30);
}
