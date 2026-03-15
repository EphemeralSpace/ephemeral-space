namespace Content.Server._ES.StationEvents.LightOverload.Components;

[RegisterComponent]
[Access(typeof(ESLightOverloadRule))]
public sealed partial class ESLightOverloadRuleComponent : Component
{
    [DataField]
    public List<EntityUid> Apcs;
}
