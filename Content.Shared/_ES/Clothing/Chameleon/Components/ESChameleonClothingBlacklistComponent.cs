using Content.Shared.Clothing.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Clothing.Chameleon.Components;

/// <summary>
/// Marker component for blacklisting clothes from <see cref="ChameleonClothingComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ESChameleonClothingBlacklistComponent : Component;
