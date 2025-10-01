using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Ephemera.Components;

/// <summary>
/// Holds dialogue for <see cref="ESEphemeraSpeakerComponent"/>.
/// The dialogue is played line by line, resetting after reaching the end or after a delay.
/// </summary>
[RegisterComponent]
[Access(typeof(ESEphemeraSpeechSystem))]
public sealed partial class ESEphemeraRandomDialogueComponent : Component
{
    /// <summary>
    /// Dataset containing the dialogue.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<LocalizedDatasetPrototype> Dialogue;

    /// <summary>
    /// Last dialogue spoken by this entity.
    /// Used to prevent repeats.
    /// </summary>
    [DataField]
    public LocId? LastDialogue;
}
