using Robust.Shared.GameStates;

namespace Content.Shared._ES.SecretIdentity.Summonable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESSecretIdentitySummonSystem))]
public sealed partial class ESSecretIdentitySummonedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? OwnerMind;

    [DataField, AutoNetworkedField]
    public LocId? ExamineString;
}
