using Robust.Shared.GameStates;

namespace Content.Shared._ES.Masks.Summonable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESMaskSummonSystem))]
public sealed partial class ESMaskSummonedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? OwnerMind;

    [DataField, AutoNetworkedField]
    public LocId? ExamineString;
}
