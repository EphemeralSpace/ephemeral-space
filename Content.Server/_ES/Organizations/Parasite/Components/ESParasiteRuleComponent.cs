using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Organizations.Parasite.Components;

[RegisterComponent]
[Access(typeof(ESParasiteRuleSystem))]
public sealed partial class ESParasiteRuleComponent : Component
{
    [DataField]
    public bool ObjectivesCompleted;

    [DataField]
    public bool SwarmStarted;

    /// <summary>
    /// Whether the "finale" has been triggered via all the objectives being completed and the timer passing.
    /// After this point,
    /// </summary>
    [DataField]
    public bool WinStarted;

    [DataField]
    public TimeSpan SwarmDelay = TimeSpan.FromSeconds(30f);

    [DataField]
    public TimeSpan WinDelay = TimeSpan.FromMinutes(1.5f);

    [DataField]
    public SoundSpecifier BurstSound = new SoundCollectionSpecifier("desecration");

    [DataField]
    public ProtoId<StartingGearPrototype> SwarmGear = "ESParasiteSwarmGear";
}

/// <summary>
/// Used for marking objectives which should be frozen when the parasite end of round condition comes.
/// </summary>
[RegisterComponent]
public sealed partial class ESParasiteWinFreezeObjectiveComponent : Component;
