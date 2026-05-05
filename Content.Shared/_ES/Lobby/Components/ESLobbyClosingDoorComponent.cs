namespace Content.Shared._ES.Lobby.Components;

/// <summary>
///     Marks a door which should be closed once the lobby is "closed" (i.e. through the cvar), and opened if it is open.
/// </summary>
[RegisterComponent]
public sealed partial class ESLobbyClosingDoorComponent : Component;
