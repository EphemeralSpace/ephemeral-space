using Content.Shared.PrototypeTable.PrototypeSelectors;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Voting.Components;

/// <summary>
/// Denotes sets of <see cref="ESVoteOption"/> that come from
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedVoteSystem))]
public sealed partial class ESEntityPrototypeVoteComponent : Component
{
    [DataField(required: true)]
    public PrototypeTableSelector<EntityPrototype> Options = new NoneSelector<EntityPrototype>();
}
