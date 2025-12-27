using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks.Phantom.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ESReincarnateMindComponent : Component
{
    /// <summary>
    /// Mob that will be spawned as the reincarnation
    /// </summary>
    [DataField]
    public EntProtoId ReincarnateEntity = "ESMobPhantom";

    /// <summary>
    /// Name used for the new mob, with the player's name passed in as $name
    /// </summary>
    [DataField]
    public LocId Name = "es-phantom-name-fmt";
}
