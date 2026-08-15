using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chat;

/// <summary>
/// Categories for chat channels for filtering on the client UI
/// </summary>
[Prototype("esChatChannelFilter")]
public sealed partial class ESChatChannelFilterPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;
}
