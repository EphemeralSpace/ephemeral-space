using Robust.Shared.Prototypes;

namespace Content.Server._ES.Multistation.Components;

[RegisterComponent]
public sealed partial class ESMultistationMapComponent : Component
{
    [DataField]
    public ProtoId<ESMultistationConfigPrototype> Config;

    [DataField]
    public bool GridsLoaded;
}
