using Content.Shared.Maps;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Light.Components;

/// <summary>
/// Applies roofs to a grid based on the tiles on the floor.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ESTileBasedRoofComponent : Component
{
    /// <summary>
    /// Which tiles count as unroofed
    /// </summary>
    // TODO: ideally this is on the prototype but for my life i cannot be fucked with it.
    [DataField]
    public HashSet<ProtoId<ContentTileDefinition>> UnRoofedTiles = new()
    {
        "Lattice", // Space structures
        "FloorGlass", // See-through tiles
        "FloorRGlass",
        "FloorAsteroidSand", // natural non-station tiles
        "PlatingAsteroid",
    };
}
