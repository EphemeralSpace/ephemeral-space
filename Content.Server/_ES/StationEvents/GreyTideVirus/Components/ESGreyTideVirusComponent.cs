using Content.Shared.Access;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.StationEvents.GreyTideVirus.Components;

/// <summary>
/// Used for a random event where an access group has it's doors force-bolted open.
/// </summary>
[RegisterComponent]
[Access(typeof(ESGreyTideVirusRule))]
public sealed partial class ESGreyTideVirusComponent : Component
{
    /// <summary>
    /// Entities with this access level will be interfered with.
    /// </summary>
    [DataField]
    public ProtoId<AccessGroupPrototype> AccessGroup = "Research";

    /// <summary>
    /// Entities with this access level will be ignored.
    /// </summary>
    [DataField]
    public List<ProtoId<AccessLevelPrototype>> Blacklist = new();

    /// <summary>
    /// Chance for a given object to be ignored by the virus
    /// </summary>
    [DataField]
    public float IgnoreChance = 0.5f;
}
