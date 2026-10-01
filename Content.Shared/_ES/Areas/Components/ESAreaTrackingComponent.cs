using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Areas.Components;

/// <summary>
/// Query component for tracking and sending updates when an entity changes areas.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESAreasSystem))]
public sealed partial class ESAreaTrackingComponent : Component
{
    /// <summary>
    /// The area this entity is inside. Can be null if not in any area.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ESAreaPrototype>? Area;
}

/// <summary>
/// Event raised on an entity <see cref="ESAreaTrackingComponent"/> when they change areas.
/// </summary>
[ByRefEvent]
public readonly record struct ESAreaChangedEvent(ProtoId<ESAreaPrototype>? OldArea, ProtoId<ESAreaPrototype>? NewArea);
