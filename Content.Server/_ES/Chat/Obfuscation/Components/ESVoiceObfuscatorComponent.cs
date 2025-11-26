namespace Content.Server._ES.Chat.Obfuscation.Components;

/// <summary>
/// This is used for clothing items which hide the name of someone speaking.
/// This version replaces it with a generic descriptor of their appearance.
/// </summary>
[RegisterComponent]
[Access(typeof(ESVoiceObfuscatorSystem))]
public sealed partial class ESVoiceObfuscatorComponent : Component;
