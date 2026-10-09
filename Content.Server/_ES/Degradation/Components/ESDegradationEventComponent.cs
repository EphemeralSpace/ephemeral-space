using Content.Shared.PrototypeTable.ValueSelector;
using Content.Shared.Whitelist;

namespace Content.Server._ES.Degradation.Components;

/// <summary>
/// Used for a generic station event that marks several random entities for queued degradation.
/// </summary>
[RegisterComponent]
[Access(typeof(ESDegradationEventSystem), Other = AccessPermissions.None)]
public sealed partial class ESDegradationEventComponent : Component
{
    /// <summary>
    /// Number of entities that are targeted for the event.
    /// </summary>
    [DataField]
    public NumberSelector Count = new ConstantNumberSelector();

    /// <summary>
    /// The component that is targeted
    /// </summary>
    [DataField(required: true)]
    public string Component = string.Empty;

    /// <summary>
    /// Whitelist to filter which entities are selected for degradation.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Blacklist to filter which entities are selected for degradation.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// If true, degradation will be immediately caused instead of simply queued.
    /// </summary>
    [DataField]
    public bool DegradeImmediately;
}
