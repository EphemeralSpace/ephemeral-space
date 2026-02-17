using Content.Shared.Preferences;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Auditions.Components;

/// <summary>
/// This is used for marking the character of components.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESCharacterComponent : Component
{
    [ViewVariables]
    public string Name => Profile.Name;

    /// <summary>
    /// Version of <see cref="Name"/> with titles and suffixes stripped out.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string BaseName;

    [DataField, AutoNetworkedField]
    public DateTime DateOfBirth;

    [DataField, AutoNetworkedField]
    public HumanoidCharacterProfile Profile;

    [DataField, AutoNetworkedField]
    public EntityUid Station;

    [DataField, AutoNetworkedField]
    public string Descriptor;

    [DataField, AutoNetworkedField]
    public string Focus;

    [DataField, AutoNetworkedField]
    public List<LocId> Likes = [];

    [DataField, AutoNetworkedField]
    public List<LocId> Dislikes = [];
}
