namespace Content.Server._ES.Breakable.Components;

[RegisterComponent]
public sealed partial class ESChangeConstructionOnBreakComponent : Component
{
    [DataField(required: true)]
    public string Node = string.Empty;
}
