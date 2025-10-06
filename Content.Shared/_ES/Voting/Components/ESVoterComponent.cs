using Robust.Shared.GameStates;

namespace Content.Shared._ES.Voting.Components;

/// <summary>
/// Marker component that denotes someone as being able to vote on <see cref="ESVoteComponent"/>.
/// Additionally used for networking vote entities without divulging them to all clients.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedVoteSystem))]
public sealed partial class ESVoterComponent : Component;
