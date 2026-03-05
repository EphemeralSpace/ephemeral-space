using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Speech;

/// <summary>
///     Randomly forces coughing onomatopoeia/interjections into sent messages, along with having a chance to randomly cut off messages entirely
/// </summary>
[RegisterComponent]
public sealed partial class ESCoughingAccentComponent : Component
{
    /// <summary>
    ///     Chance, per interjection that rolls, that the message will be cut off, ending there.
    /// </summary>
    [DataField]
    public float CutOffMessageChancePerInterjection = 1f / 4f;

    [DataField]
    public float InterjectionChancePerCharacter = 0.07f;

    [DataField]
    public float MinMessageLength = 4;

    [DataField]
    public float InterjectionCapitalizeChance = 1f / 3f;

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> CoughingInterjectionDataset = "ESCoughingInterjections";
}
