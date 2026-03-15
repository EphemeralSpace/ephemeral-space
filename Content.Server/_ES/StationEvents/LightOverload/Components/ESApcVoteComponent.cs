namespace Content.Server._ES.StationEvents.LightOverload.Components;

[RegisterComponent]
[Access(typeof(ESLightOverloadRule))]
public sealed partial class ESApcVoteComponent : Component
{
    [DataField]
    public int Count = 4;
}
