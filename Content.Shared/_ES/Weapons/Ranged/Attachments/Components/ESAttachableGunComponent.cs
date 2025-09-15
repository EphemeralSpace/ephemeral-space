using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Weapons.Ranged.Attachments.Components;

/// <summary>
/// Used to hold data for guns which can have attachments added onto them.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedGunAttachmentsSystem))]
public sealed partial class ESAttachableGunComponent : Component
{
    /// <summary>
    /// The slots that can have attachments added.
    /// </summary>
    [DataField]
    public List<ESGunAttachmentSlot> Slots = new();
}

[Serializable, NetSerializable]
[DataDefinition]
public partial record struct ESGunAttachmentSlot
{
    /// <summary>
    /// The name of this slot
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Container associated with the slot where the item is stored.
    /// </summary>
    [DataField(required: true)]
    public string ContainerId;

    /// <summary>
    /// Tags used to whitelist which particular attachments can be used with this slot.
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<TagPrototype>> AttachmentTags = new();
}
