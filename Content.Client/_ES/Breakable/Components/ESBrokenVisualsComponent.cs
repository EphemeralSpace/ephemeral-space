using Robust.Shared.Utility;

namespace Content.Client._ES.Breakable.Components;

[RegisterComponent]
public sealed partial class ESBrokenVisualsComponent : Component
{
    [DataField(required: true)]
    public ResPath BaseRSI;

    [DataField(required: true)]
    public ResPath BrokenRSI;
}
