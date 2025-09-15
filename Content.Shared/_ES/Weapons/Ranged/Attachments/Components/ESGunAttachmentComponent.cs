using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Weapons.Ranged.Attachments.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedGunAttachmentsSystem))]
public sealed partial class ESGunAttachmentComponent : Component
{
    /// <summary>
    /// Tags matched to <see cref="ESGunAttachmentSlot.AttachmentTags"/> to see if an attachment can be inserted into a slot.
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<TagPrototype>> AttachmentTags = new();
}
