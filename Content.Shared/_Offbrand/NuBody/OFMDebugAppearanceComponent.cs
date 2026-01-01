using Content.Shared.Humanoid;
using Robust.Shared.GameStates;

namespace Content.Shared._Offbrand.NuBody;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OFMDebugAppearanceComponent : Component
{
    [DataField, AutoNetworkedField]
    public HumanoidCharacterAppearance? Appearance;
}
