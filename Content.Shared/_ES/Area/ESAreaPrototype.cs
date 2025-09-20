using Content.Shared._ES.Area.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Area;

/// <summary>
/// Generic marker used in conjunction with <see cref="ESAreaMarkerComponent"/> for querying areas on the station.
/// </summary>
[Prototype("esArea")]
public sealed partial class ESAreaPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;
}
