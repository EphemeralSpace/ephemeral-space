using Robust.Shared.GameStates;

namespace Content.Shared._ES.Interaction;

/// <summary>
///     Okay, so previously, things like windows & firelocks & shutters and what have you
///     would have WallMount with an arc of 360, specifically -just- to tell the interaction system
///     "please let people interact with me even if im on top of a wall", and none of the other wallmount functionality
///     Since I added conditional rendering to wallmounts based on their direction (like SS13), I would greatly prefer
///     if the client did not have to enumerate every window and firelock just to ignore them anyway
///     So I chose to special case their behavior into a new component rather than reusing wallmount
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ESInteractionIgnoreAnchoredInTileComponent : Component
{
}
