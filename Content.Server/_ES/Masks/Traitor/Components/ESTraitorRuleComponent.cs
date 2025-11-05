using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Traitor.Components;

/// <summary>
/// Used to manage traitor rule logic regarding the nuke detonation
/// </summary>
[RegisterComponent]
[Access(typeof(ESTraitorRuleSystem))]
public sealed partial class ESTraitorRuleComponent : Component
{
    /// <summary>
    /// Grids that make up the syndicate base.
    /// </summary>
    [DataField]
    public List<EntityUid> BaseGrids = new();

    /// <summary>
    /// Effect spawned when teleporting a player to the base
    /// </summary>
    [DataField]
    public EntProtoId TeleportEffect = "ESTeleportEffectSyndie";
}
