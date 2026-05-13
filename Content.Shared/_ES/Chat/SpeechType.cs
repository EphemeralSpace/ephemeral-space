using Robust.Shared.Serialization;

namespace Content.Shared._ES.Chat;

[Serializable, NetSerializable]
public enum SpeechType : byte
{
    // Does not display
    None,

    Emote,
    Say,
    Whisper,
    Looc,
}
