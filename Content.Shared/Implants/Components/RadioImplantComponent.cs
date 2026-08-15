using Content.Shared._ES.Chat;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Implants.Components;

/// <summary>
/// Gives the user access to a given channel without the need for a headset.
/// </summary>
[RegisterComponent]
public sealed partial class RadioImplantComponent : Component
{
    /// <summary>
    /// The radio channel(s) to grant access to.
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<ESChatChannelPrototype>> RadioChannels = new();
}
