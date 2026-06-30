using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.SecretIdentity.Tragedian.Components;

[RegisterComponent]
[Access(typeof(ESEmbodyThemeObjectiveSystem))]
public sealed partial class ESEmbodyThemeObjectiveComponent : Component
{
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> ThemeDataset = "TragedianThemes";

    [DataField]
    public string Theme = string.Empty;

    [DataField]
    public LocId Title = "es-embody-theme-objective-title";

    [DataField]
    public EntProtoId VoteEntity = "ESVoteTragedianSuccess";

    [DataField]
    public LocId VoteTitle = "es-embody-theme-vote-title";
}
