using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server._ES.Filth.Components;

[RegisterComponent]
public sealed partial class ESMiasmaGeneratorGameRuleComponent : Component
{
    [DataField]
    public EntityTableSelector PestTable = new NoneSelector();
}
