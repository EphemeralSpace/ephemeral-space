using Robust.Shared.Audio;

namespace Content.Server._ES.Ephemera.Components;

/// <summary>
/// This is used for an in-game "Extra" that can be spoken to.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(ESEphemeraSpeechSystem))]
public sealed partial class ESEphemeraSpeakerComponent : Component
{
    /// <summary>
    /// The time at which regular speech can happen again
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextCanSpeakTime;

    /// <summary>
    /// Sound that plays when dialogue occurs.
    /// </summary>
    [DataField]
    public SoundSpecifier? SpeakSound;
}

/// <summary>
/// Event raised on an entity to get the dialogue spoken by a particular <see cref="ESEphemeraSpeakerComponent"/>
/// </summary>
[ByRefEvent]
public record struct ESEphemeraGetDialogueEvent()
{
    /// <summary>
    /// The dialogue that will be spoken. This text will be displayed directly.
    /// </summary>
    public string? Line = null;
    public bool Handled => Line != null;
}
