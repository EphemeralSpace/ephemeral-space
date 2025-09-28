namespace Content.Server._ES.Ephemera.Components;

/// <summary>
/// Holds dialogue for <see cref="ESEphemeraSpeakerComponent"/>.
/// The dialogue is played line by line, resetting after reaching the end or after a delay.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(ESEphemeraSpeechSystem))]
public sealed partial class ESEphemeraSequentialDialogueComponent : Component
{
    /// <summary>
    /// The dialogue that this character speaks when interacted with, in order
    /// </summary>
    [DataField]
    public List<LocId> Dialogue = new();

    /// <summary>
    /// A counter indicating which element of <see cref="Dialogue"/> is to be spoken
    /// </summary>
    [DataField]
    public int DialogueIndex;

    /// <summary>
    /// The time at which <see cref="DialogueIndex"/> is automatically reset to 0.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan DialogueResetTime;

    /// <summary>
    /// The delay between the last time this extra spoke and when <see cref="DialogueIndex"/> is reset.
    /// </summary>
    [DataField]
    public TimeSpan DialogueResetDelay = TimeSpan.FromSeconds(10);
}
