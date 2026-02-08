using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks.Components;

/// <summary>
///     This is used for blacklisting certain masks from targeted objectives.
/// </summary>
[RegisterComponent]
public sealed partial class ESTargetMaskBlacklistComponent : Component
{
    /// <summary>
    /// A blacklist of masks that cannot be targeted.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<ESMaskPrototype>> MaskBlacklist;
}
