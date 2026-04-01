using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Troupes.Parasite.Components;

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
    public TimeSpan SwarmDelay = TimeSpan.FromMinutes(1);

    [DataField]
    public TimeSpan WinDelay = TimeSpan.FromMinutes(2.5);

    [DataField]
    public SoundSpecifier BurstSound = new SoundCollectionSpecifier("desecration");

    [DataField]
    public ProtoId<StartingGearPrototype> SwarmGear = "ESParasiteSwarmGear";
}
