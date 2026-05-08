using Content.Shared.Popups;

namespace Content.Shared.Singularity.EntitySystems;

/// <summary>
/// Shared part of SingularitySingularityGeneratorSystem
/// </summary>
public abstract partial class SharedSingularityGeneratorSystem : EntitySystem
{
    #region Dependencies
    [Dependency] protected SharedPopupSystem PopupSystem = default!;
    #endregion Dependencies
}
