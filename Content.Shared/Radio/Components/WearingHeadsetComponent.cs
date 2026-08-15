using Robust.Shared.GameStates;

namespace Content.Shared.Radio.Components;

/// <summary>
///     This component is used to tag players that are currently wearing an ACTIVE headset.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WearingHeadsetComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Headset;
}
