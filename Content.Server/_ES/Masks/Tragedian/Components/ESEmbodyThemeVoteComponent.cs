namespace Content.Server._ES.Masks.Tragedian.Components;

[RegisterComponent]
[Access(typeof(ESEmbodyThemeObjectiveSystem))]
public sealed partial class ESEmbodyThemeVoteComponent : Component
{
    [DataField]
    public EntityUid Objective;
}
