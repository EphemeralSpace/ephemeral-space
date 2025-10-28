using Content.Shared._ES.Voting.Components;
using Content.Shared.Access;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.StationEvents.GreyTideVirus.Components;

/// <summary>
/// <see cref="ESVoteComponent"/> for a random access group.
/// </summary>
[RegisterComponent]
[Access(typeof(ESGreyTideVirusRule))]
public sealed partial class ESAccessGroupVoteComponent : Component
{
    /// <summary>
    /// Accesses that can be selected
    /// </summary>
    [DataField]
    public List<ProtoId<AccessGroupPrototype>> Options = new();

    /// <summary>
    /// Number of options that will be selected
    /// </summary>
    [DataField]
    public int Count = 4;
}
