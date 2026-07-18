namespace Content.Client._ES.LocalPlayer;

/// <summary>
///     Always added to the entity the client is currently controlling, for directed event subscribing purposes.
/// </summary>
[RegisterComponent]
public sealed partial class ESLocalPlayerMarkerComponent : Component;
