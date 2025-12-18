using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Mind.Components;

/// <summary>
/// Denotes an entity which is given special visuals when
/// the owning player has left the game or no longer occupies the body.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedMindVisualsSystem), Other = AccessPermissions.None)]
public sealed partial class ESMindVisualsComponent : Component
{
    /// <summary>
    /// The user that this entity is tied to. can be null.
    /// </summary>
    [DataField]
    public NetUserId? AssociatedUser;

    /// <summary>
    /// Color to set the entity to when no mind is present
    /// </summary>
    [DataField]
    public Color NoMindColor = Color.FromHex("#6c7da088");
}

[Serializable, NetSerializable]
public enum ESMindVisuals : byte
{
    HasMind,
    InGame,
}
