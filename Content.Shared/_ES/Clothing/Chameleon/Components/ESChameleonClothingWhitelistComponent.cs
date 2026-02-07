using Content.Shared.Clothing.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Clothing.Chameleon.Components;

/// <summary>
/// Marker component for whitelisting clothes for <see cref="ChameleonClothingComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ESChameleonClothingWhitelistComponent : Component;
