using Robust.Shared.GameStates;

namespace Content.Shared._ES.Breakable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESBreakableSystem))]
public sealed partial class ESBreakablePointLightComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color BaseColor = Color.White;

    [DataField]
    public Color BrokenColor = Color.White;

    [DataField]
    public bool DisableOnBroken;
}
