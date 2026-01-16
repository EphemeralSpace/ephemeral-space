using Content.Shared._Citadel.Utilities;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Masks.Pickpocket;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESPickpocketSystem))]
public sealed partial class ESPickpocketerComponent : Component
{
    [AutoNetworkedField]
    public SmallRandom Rng;
}
