namespace Content.Server._ES.Stagehand.Components;

/// <summary>
/// Used for entities which show the player's username when they are occupied, rather than an IC name.
/// </summary>
[RegisterComponent]
[Access(typeof(ESUsernameEntityNameSystem))]
public sealed partial class ESUsernameEntityNameComponent : Component;
