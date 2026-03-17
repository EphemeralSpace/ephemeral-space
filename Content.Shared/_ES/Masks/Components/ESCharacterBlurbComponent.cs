using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Masks.Components;

/// <summary>
/// Holds a character blurb/summary for the mind
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESCharacterBlurbComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<FormattedMessage> Info = [];
}
