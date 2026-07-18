using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Radstorm.Components;

/// <summary>
/// Used for a machine which changes the speed of the radstorm based on whether it has power.
/// </summary>
[RegisterComponent]
public sealed partial class ESRadstormModifierMachineComponent : Component
{
    /// <summary>
    /// Indicator if <see cref="Modifier"/> should be applied.
    /// </summary>
    [DataField]
    public bool Enabled;

    /// <summary>
    /// Additive modifier applied to radstorm speed when <see cref="Enabled"/> is true
    /// </summary>
    [DataField]
    public float Modifier = 0.2f;

    /// <summary>
    /// Announcement broadcast when enabled
    /// </summary>
    [DataField(required: true)]
    public LocId EnableAnnouncement;

    /// <summary>
    /// Announcement broadcast when disabled.
    /// </summary>
    [DataField(required: true)]
    public LocId DisableAnnouncement;

    [DataField]
    public SoundSpecifier AnnouncementSoundEnabled = new SoundPathSpecifier("/Audio/_ES/Announcements/attention_high.ogg");

    [DataField]
    public SoundSpecifier AnnouncementSoundDisabled = new SoundPathSpecifier("/Audio/_ES/Announcements/attention_high.ogg");
}

[Serializable, NetSerializable]
public enum ESRadstormModifierMachineVisuals : byte
{
    Enabled,
}
