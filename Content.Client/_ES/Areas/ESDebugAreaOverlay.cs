using System.Numerics;
using Content.Shared._ES.Areas;
using Robust.Client.Debugging.Overlays;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._ES.Areas;

public sealed partial class ESDebugAreaOverlay : TileDebugOverlay
{
    [Dependency] private IPrototypeManager _protoMan = default!;
    private ESAreasSystem _areas = default!;

    protected override void Init()
    {
        _areas = Entity.System<ESAreasSystem>();
    }

    protected override string? GetText(Vector2i indices, Entity<MapGridComponent> grid)
    {
        if (!_areas.TryGetArea(grid, indices, out var areaId))
            return null;
        return areaId;
    }

    protected override string? GetTooltip(Vector2 mousePos, Vector2i indices, Entity<MapGridComponent> grid)
    {
        return null;
    }

    protected override (Color Fill, Color Border)? GetColor(Vector2i indices, Entity<MapGridComponent> grid)
    {
        if (!_areas.TryGetArea(grid, indices, out var areaId))
            return null;

        var area = _protoMan.Index(areaId);
        return (area.DebugColor.WithAlpha(0.1f), area.DebugColor);
    }
}
