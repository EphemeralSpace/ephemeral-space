using System.Numerics;
using Content.Client.UserInterface.Systems.Viewport;
using Robust.Client.Debugging.Overlays;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Audio.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Client._ES.SoundOcclusion;

public sealed class TomenoSoundOcclusionOverlay : TileDebugOverlay
{
    [Dependency] private IEntityManager _entityManager = default!;
    // [Dependency] private IMapManager _mapManager = default!;

    private readonly SharedMapSystem _mapSystem;
    private readonly TomenoSoundOcclusionSystem _occlusionSystem;
    private readonly TransformSystem _transformSystem;

    private EntityQuery<MapGridComponent> _mapGridQuery;
    private EntityQuery<AudioComponent> _audioQuery;

    public TomenoSoundOcclusionOverlay() : base()
    {
        IoCManager.InjectDependencies(this);
        _occlusionSystem = _entityManager.System<TomenoSoundOcclusionSystem>();
        _mapSystem = _entityManager.System<SharedMapSystem>();
        _transformSystem = _entityManager.System<TransformSystem>();

        _mapGridQuery = _entityManager.GetEntityQuery<MapGridComponent>();
        _audioQuery = _entityManager.GetEntityQuery<AudioComponent>();

        base.Transform = _entityManager.System<SharedTransformSystem>();
        base.Map = _entityManager.System<MapSystem>();
        base.Lookup = _entityManager.System<EntityLookupSystem>();
        var font = Cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf");
        base.Font = new VectorFont(font, 8);
    }

    // private (TomenoSoundOcclusionSystem.SoundstagePathTile?, Vector2i?) PosToSoundstage(Vector2i indices, Entity<MapGridComponent> grid)
    // {
    //     if (_soundSystem.IsSoundstageValid())
    //     {
    //         if (grid.Owner != _soundSystem.LastGridUid)
    //         {
    //             if (_mapGridQuery.TryGetComponent(_soundSystem.LastGridUid, out var soundGrid) || soundGrid == null || _soundSystem.LastGridUid == null)
    //                 return (null, null);
    //
    //             var finalIndices = _mapSystem.WorldToTile(_soundSystem.LastGridUid.Value, soundGrid, _mapSystem.GridTileToWorldPos(grid.Owner, grid, indices));
    //             if (_soundSystem.SoundstagePaths.TryGetValue(finalIndices, out var soundStage))
    //             {
    //                 return (soundStage, finalIndices);
    //             }
    //         }
    //         else
    //         {
    //             if (_soundSystem.SoundstagePaths.TryGetValue(indices, out var soundStage))
    //             {
    //                 return (soundStage, indices);
    //             }
    //         }
    //     }
    //
    //     return (null, null);
    // }

    protected override string? GetText(Vector2i indices, Entity<MapGridComponent> grid)
    {
        // var (soundstage, tile) = PosToSoundstage(indices, grid);
        // if (soundstage == null)
        //     return null;
        // return $"{soundstage.Distance:N1}";

        //var indiceWorld = _mapSystem.GridTileToWorldPos();
        if (_occlusionSystem.CurrentSoundPaths == null)
            return null;

        var world = _mapSystem.GridTileToWorldPos(grid.Owner, grid, indices);
        var soundTile = _occlusionSystem.CurrentSoundPaths.Stage.WorldToTile(world);
        if (!_occlusionSystem.CurrentSoundPaths.Paths.TryGetValue(soundTile, out var pathTile))
            return null;
        if (pathTile == null)
            return "•";

        string[,] unicodes =
        {
            {"/", "\u2193", "\\"},
            {"\u2190", "•", "\u2192"},
            {"\\", "\u2191", "/"},
        };

        var offset = pathTile.Value - soundTile + Vector2i.One;
        return unicodes[offset.Y, offset.X];
    }


    void DrawSounds(DrawingHandleScreen handle, IViewportControl viewport)
    {
        var audioQuery = _entityManager.EntityQueryEnumerator<AudioComponent>();
        while (audioQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.Global || !comp.Started)
                continue;

            if (!_entityManager.TryGetComponent<TransformComponent>(uid, out var transform))
                continue;

            var pos = viewport.WorldToScreen(_transformSystem.GetWorldPosition(transform));
            handle.DrawString(Font, pos + new Vector2(16, 16), $"{comp.Occlusion:N2}");
        }
    }

    protected override void DrawTooltip(DrawingHandleScreen handle)
    {
        if (_occlusionSystem.CurrentSoundPaths == null)
            return;

        var mousePos = Input.MouseScreenPosition;
        if (!mousePos.IsValid)
            return;

        if (Ui.MouseGetControl(mousePos) is not IViewportControl viewport)
            return;

        DrawSounds(handle, viewport);

        var coords = viewport.PixelToMap(mousePos.Position);

        var emitterPos = _occlusionSystem.CurrentSoundPaths.Stage.WorldToLocal(coords.Position);
        var emitterTile = _occlusionSystem.CurrentSoundPaths.Stage.LocalToTile(emitterPos);

        var foundPath = _occlusionSystem.FindPath(emitterPos);
        var playerPos = _occlusionSystem.LastLocalPos;

        if (foundPath == null || !playerPos.HasValue)
            return;

        var pathPositions = new List<Vector2>();
        pathPositions.Add(emitterPos);

        for (var i = 0; i < foundPath.Path.Count; i++)
        {
            pathPositions.Add(foundPath.Path[i] + Vector2.One / 2);
        }

        pathPositions.Add(playerPos.Value);

        for (var i = 1; i < pathPositions.Count; i++)
        {
            var v1 = viewport.WorldToScreen(_occlusionSystem.CurrentSoundPaths.Stage.LocalToWorld(pathPositions[i-1] ));
            var v2 = viewport.WorldToScreen(_occlusionSystem.CurrentSoundPaths.Stage.LocalToWorld(pathPositions[i]));
            handle.DrawLine(v1, v2, Color.Green);
        }

        var distance = foundPath.PortalDistance;
        if (foundPath.ListenerPortal.HasValue && foundPath.EmitterPortal.HasValue)
        {
            distance += Vector2.Distance(emitterPos, foundPath.EmitterPortal.Value);
            distance += Vector2.Distance(playerPos.Value, foundPath.ListenerPortal.Value);
        }
        else
        {
            distance = Vector2.Distance(emitterPos, playerPos.Value);
        }

        handle.DrawString(Font, mousePos.Position + Vector2.UnitX * 8, $"{distance:N1}");

        // if (!_mapSystem.TryFindGridAt(coords, out var grid, out var comp))
        //     return;

        // var local = Map.WorldToLocal(grid, comp, coords.Position);
        // var x = (int) Math.Floor(local.X / comp.TileSize);
        // var y = (int) Math.Floor(local.Y / comp.TileSize);
        // var indices = new Vector2i(x, y);
        //
        // //DrawTooltip(handle, mousePos.Position, local, indices, (grid, comp));
        // var (valid, finalDistance, rawPathDistance, pathTiles, pathPoints) = _soundSystem.FindSoundPath(local, (x, y));
        // if (!valid || pathTiles is null || pathPoints is null)
        //     return;
        //
        // //var lineHeight = Font.GetLineHeight(1f);
        // //var offset = new Vector2(0, lineHeight);
        // //handle.DrawString(Font, mouseScreen - offset, text);
        // for (var i = 1; i < pathTiles.Count; i++)
        // {
        //     var v1 = viewport.WorldToScreen(Map.GridTileToWorldPos(grid, comp, pathTiles[i-1]));
        //     var v2 = viewport.WorldToScreen(Map.GridTileToWorldPos(grid, comp, pathTiles[i]));
        //     handle.DrawLine(v1, v2, Color.Blue);
        // }
        //
        // for (var i = 1; i < pathPoints.Count; i++)
        // {
        //     var v1 = viewport.WorldToScreen(Map.LocalToWorld(grid, comp, pathPoints[i-1] ));
        //     var v2 = viewport.WorldToScreen(Map.LocalToWorld(grid, comp, pathPoints[i]));
        //     handle.DrawLine(v1, v2, Color.Purple);
        // }
    }

    protected override string? GetTooltip(Vector2 mousePos, Vector2i indices, Entity<MapGridComponent> grid)
    {
        return null;
    }

    protected override (Color Fill, Color Border)? GetColor(Vector2i indices, Entity<MapGridComponent> grid)
    {
        // var (soundstage, tile) = PosToSoundstage(indices, grid);
        // if (soundstage == null || tile == null)
        //     return null;
        // var soundable = _soundSystem.Soundstage.GetValueOrDefault(tile.Value, false);
        // var outlineColor = soundable ? Color.White.WithAlpha(0.3f) : Color.Black.WithAlpha(0.6f);
        // var fillColor = Color.FromHsv(new Vector4(0.5f - (soundstage.Distance / 30f), 1f, 1f, 0.05f));
        // return (fillColor, outlineColor);
        if (_occlusionSystem.CurrentSoundPaths == null)
            return null;

        var world = _mapSystem.GridTileToWorldPos(grid.Owner, grid, indices);
        var soundTile = _occlusionSystem.CurrentSoundPaths.Stage.WorldToTile(world);
        if (!_occlusionSystem.CurrentSoundPaths.Paths.TryGetValue(soundTile, out var pathTile))
            return null;

        if (pathTile == null)
            return null;

        return (Color.Transparent, Color.White.WithAlpha(0.25f));
    }
}
