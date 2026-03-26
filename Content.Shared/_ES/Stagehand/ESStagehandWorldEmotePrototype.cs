using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Stagehand;

/// <summary>
///     Sound and message that stagehands can play with some cooldown, which players ingame might be able to hear.
/// </summary>
[Prototype("esStagehandWorldEmote")]
public sealed partial class ESStagehandWorldEmotePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; set; } = default!;

    /// <summary>
    ///     Message to send to stagehand chat when this emote is played.
    ///     Passes in the name of the performer as $entity.
    /// </summary>
    [DataField(required: true)]
    public LocId Message;

    [DataField(required: true)]
    public SoundSpecifier Sound = default!;
}

/// <summary>
///     Action event for playing a world emote.
/// </summary>
public sealed partial class ESStagehandWorldEmoteEvent : InstantActionEvent
{
    [DataField(required: true)]
    public ProtoId<ESStagehandWorldEmotePrototype> Emote = default!;
}
