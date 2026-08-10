using System.Numerics;
using Robust.Client.Debugging.Overlays;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Client._ES.SoundOcclusion;

public sealed class TomenoSoundOcclusionOverlay : TileDebugOverlay
{
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IMapManager _mapManager = default!;

    private readonly SharedMapSystem _mapSystem;
    private readonly TomenoSoundOcclusionSystem _soundSystem;

    private EntityQuery<MapGridComponent> _mapGridQuery;

    public TomenoSoundOcclusionOverlay() : base()
    {
        IoCManager.InjectDependencies(this);
        _soundSystem = _entityManager.System<TomenoSoundOcclusionSystem>();
        _mapSystem = _entityManager.System<SharedMapSystem>();

        _mapGridQuery = _entityManager.GetEntityQuery<MapGridComponent>();
    }

    private (TomenoSoundOcclusionSystem.SoundstagePathTile?, Vector2i?) PosToSoundstage(Vector2i indices, Entity<MapGridComponent> grid)
    {
        if (_soundSystem.IsSoundstageValid())
        {
            if (grid.Owner != _soundSystem.LocalGridUid)
            {
                if (_mapGridQuery.TryGetComponent(_soundSystem.LocalGridUid, out var soundGrid) || soundGrid == null || _soundSystem.LocalGridUid == null)
                    return (null, null);

                var finalIndices = _mapSystem.WorldToTile(_soundSystem.LocalGridUid.Value, soundGrid, _mapSystem.GridTileToWorldPos(grid.Owner, grid, indices));
                if (_soundSystem.SoundstagePaths.TryGetValue(finalIndices, out var soundStage))
                {
                    return (soundStage, finalIndices);
                }
            }
            else
            {
                if (_soundSystem.SoundstagePaths.TryGetValue(indices, out var soundStage))
                {
                    return (soundStage, indices);
                }
            }
        }

        return (null, null);
    }

    protected override string? GetText(Vector2i indices, Entity<MapGridComponent> grid)
    {
        var (soundstage, tile) = PosToSoundstage(indices, grid);
        if (soundstage == null)
            return null;
        return $"{soundstage.Distance:N1}";
    }

    protected override void DrawTooltip(DrawingHandleScreen handle)
    {
        var mousePos = Input.MouseScreenPosition;
        if (!mousePos.IsValid)
            return;

        if (Ui.MouseGetControl(mousePos) is not IViewportControl viewport)
            return;

        var coords = viewport.PixelToMap(mousePos.Position);

        if (!MapMan.TryFindGridAt(coords, out var grid, out var comp))
            return;

        var local = Map.WorldToLocal(grid, comp, coords.Position);
        var x = (int) Math.Floor(local.X / comp.TileSize);
        var y = (int) Math.Floor(local.Y / comp.TileSize);
        var indices = new Vector2i(x, y);

        //DrawTooltip(handle, mousePos.Position, local, indices, (grid, comp));
        var (valid, finalDistance, rawPathDistance, pathTiles, pathPoints) = _soundSystem.FindSoundPath(local, (x, y));
        if (!valid || pathTiles is null || pathPoints is null)
            return;

        //var lineHeight = Font.GetLineHeight(1f);
        //var offset = new Vector2(0, lineHeight);
        //handle.DrawString(Font, mouseScreen - offset, text);
        for (var i = 1; i < pathTiles.Count; i++)
        {
            var v1 = viewport.WorldToScreen(Map.GridTileToWorldPos(grid, comp, pathTiles[i-1]));
            var v2 = viewport.WorldToScreen(Map.GridTileToWorldPos(grid, comp, pathTiles[i]));
            handle.DrawLine(v1, v2, Color.Blue);
        }

        for (var i = 1; i < pathPoints.Count; i++)
        {
            var v1 = viewport.WorldToScreen(Map.LocalToWorld(grid, comp, pathPoints[i-1] ));
            var v2 = viewport.WorldToScreen(Map.LocalToWorld(grid, comp, pathPoints[i]));
            handle.DrawLine(v1, v2, Color.Purple);
        }
    }

    protected override string? GetTooltip(Vector2 mousePos, Vector2i indices, Entity<MapGridComponent> grid)
    {
        return null;
    }

    protected override (Color Fill, Color Border)? GetColor(Vector2i indices, Entity<MapGridComponent> grid)
    {
        var (soundstage, tile) = PosToSoundstage(indices, grid);
        if (soundstage == null || tile == null)
            return null;
        var soundable = _soundSystem.Soundstage.GetValueOrDefault(tile.Value, false);
        var outlineColor = soundable ? Color.White.WithAlpha(0.3f) : Color.Black.WithAlpha(0.6f);
        var fillColor = Color.FromHsv(new Vector4(0.5f - (soundstage.Distance / 30f), 1f, 1f, 0.05f));
        return (fillColor, outlineColor);
    }
}
