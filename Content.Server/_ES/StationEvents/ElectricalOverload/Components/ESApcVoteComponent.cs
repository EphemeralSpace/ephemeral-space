namespace Content.Server._ES.StationEvents.ElectricalOverload.Components;

[RegisterComponent]
[Access(typeof(ESElectricalOverloadRule))]
public sealed partial class ESApcVoteComponent : Component
{
    [DataField]
    public int Count = 6;
}
