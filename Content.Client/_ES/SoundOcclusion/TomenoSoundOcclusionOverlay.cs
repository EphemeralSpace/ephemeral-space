using System.Numerics;
using Content.Client.UserInterface.Systems.Viewport;
using Robust.Client.Audio;
using Robust.Client.Debugging.Overlays;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Audio.Components;
using Robust.Shared.Console;
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

    protected override string? GetText(Vector2i indices, Entity<MapGridComponent> grid)
    {
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

        var emitterPos = _occlusionSystem.CurrentSoundPaths.Stage.WorldToStage(coords.Position);
        var emitterTile = _occlusionSystem.CurrentSoundPaths.Stage.StageToTile(emitterPos);

        var foundPath = _occlusionSystem.FindPath(emitterPos);
        var playerPos = _occlusionSystem.LastLocalPos;

        if (foundPath == null || !playerPos.HasValue)
            return;

        var pathPositions = new List<Vector2>();
        pathPositions.Add(emitterPos);

        if (foundPath.EmitterPortal.HasValue && foundPath.ListenerPortal.HasValue)
        {
            pathPositions.Add(foundPath.EmitterPortal.Value);

            var curTile = _occlusionSystem.CurrentSoundPaths.Stage.StageToTile(foundPath.EmitterPortal.Value);
            var stopTile = _occlusionSystem.CurrentSoundPaths.Stage.StageToTile(foundPath.ListenerPortal.Value);

            while (curTile != stopTile)
            {
                var nextTile = _occlusionSystem.CurrentSoundPaths.Paths.GetValueOrDefault(curTile);
                if (nextTile == null || nextTile == stopTile)
                    break;
                pathPositions.Add(nextTile.Value + new Vector2(0.5f, 0.5f));
                curTile = nextTile.Value;
            }

            pathPositions.Add(foundPath.ListenerPortal.Value);
        }

        pathPositions.Add(playerPos.Value);

        for (var i = 1; i < pathPositions.Count; i++)
        {
            var v1 = viewport.WorldToScreen(_occlusionSystem.CurrentSoundPaths.Stage.StageToWorld(pathPositions[i-1] ));
            var v2 = viewport.WorldToScreen(_occlusionSystem.CurrentSoundPaths.Stage.StageToWorld(pathPositions[i]));
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
    }

    protected override string? GetTooltip(Vector2 mousePos, Vector2i indices, Entity<MapGridComponent> grid)
    {
        return null;
    }

    protected override (Color Fill, Color Border)? GetColor(Vector2i indices, Entity<MapGridComponent> grid)
    {
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
