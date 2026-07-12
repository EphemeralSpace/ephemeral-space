namespace Content.Server._ES.SecretIdentity.Summonable.Components;

/// <summary>
/// Used for an objective where a certain amount of summonable objects must be held by other players.
/// </summary>
[RegisterComponent]
[Access(typeof(ESHoldSummonableObjectiveSystem))]
public sealed partial class ESHoldSummonableObjectiveComponent : Component;
