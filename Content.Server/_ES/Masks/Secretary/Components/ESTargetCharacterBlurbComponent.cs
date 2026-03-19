using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Secretary.Components;

/// <summary>
/// Used for a component which adds a character blurb to anyone who has the objective based on the target.
/// </summary>
[RegisterComponent]
public sealed partial class ESTargetCharacterBlurbComponent : Component
{
    [DataField]
    public string Blurb = string.Empty;

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> TargetFormatDataset = "ESHaterTargetDataset";

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> ContextDataset = "ESHaterContextDataset";
}
