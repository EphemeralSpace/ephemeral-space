using Content.Shared.Preferences;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Auditions.Components;

/// <summary>
/// This is used for marking the character of components.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESCharacterComponent : Component
{
    [ViewVariables]
    public string Name => Profile.Name;

    [DataField, AutoNetworkedField]
    public DateTime DateOfBirth;

    [DataField, AutoNetworkedField]
    public List<LocId> PersonalityTraits = new();

    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, ProtoId<ESRelationshipPrototype>> Relationships = new ();

    [DataField, AutoNetworkedField]
    public ProtoId<ESBackgroundPrototype> Background;

    [DataField, AutoNetworkedField]
    public List<EntityUid> Memories = new ();

    [DataField, AutoNetworkedField]
    public HumanoidCharacterProfile Profile;

    [DataField, AutoNetworkedField]
    public EntityUid Station;
}
