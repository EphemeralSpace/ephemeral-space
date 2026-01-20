using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Shared._ES.Actions;

[RegisterComponent]
public sealed partial class ESRandomActionGrantComponent : Component
{
    /// <summary>
    /// The objectives that this troupe gives to its members
    /// </summary>
    [DataField]
    public EntityTableSelector Actions = new NoneSelector();
}
