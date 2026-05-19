using Robust.Shared.GameStates;

namespace Content.Shared._ES.Voting.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedVoteSystem))]
public sealed partial class ESBooleanVoteComponent : Component;
