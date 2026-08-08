using Robust.Shared.Serialization;

namespace Content.Shared._ES.Chat;

[Serializable, NetSerializable]
public enum SpeechType : byte
{
    Emote,
    Say,
    Whisper,
    Looc,
}
