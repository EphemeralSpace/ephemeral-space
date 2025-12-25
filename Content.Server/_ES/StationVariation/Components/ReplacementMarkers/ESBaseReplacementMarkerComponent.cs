using Robust.Shared.Prototypes;

namespace Content.Server._ES.StationVariation.Components.ReplacementMarkers;

// todo mirror support overriding prototypes when you need that too
/// <summary>
///     Base class for entity replacement markers, which allows you to disable replacement per entity.
/// </summary>
/// <remarks>
///     Disabling replacement is done for ents that inherit from something with a marker, which don't want to be replaced.
/// </remarks>
public abstract partial class ESBaseReplacementMarkerComponent : Component
{
    [DataField]
    public bool Replace = true;
}
