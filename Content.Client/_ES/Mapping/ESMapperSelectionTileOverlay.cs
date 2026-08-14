using System.Numerics;
using Content.Client.UserInterface.Systems.Chat;
using Content.Shared._ES.Chat;
using Content.Shared._ES.Mapping;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Map;

namespace Content.Client._ES.Mapping;

public sealed partial class ESMapperSelectionTileOverlay : GridOverlay
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

    private readonly ESSharedChatSystem _chat;
    private readonly ESSelectionSystem _selection;
    private readonly SharedTransformSystem _xform;
    private readonly SharedMapSystem _map;

    public ESMapperSelectionTileOverlay()
    {
        IoCManager.InjectDependencies(this);
        _chat = _entManager.System<ESSharedChatSystem>();
        _selection = _entManager.System<ESSelectionSystem>();
        _xform = _entManager.System<SharedTransformSystem>();
        _map = _entManager.System<SharedMapSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return;

        var owner = Grid.Owner;
        var grid = Grid.Comp;

        if (!_entManager.TryGetComponent(owner, out TransformComponent? gridXform) ||
            gridXform.MapID != args.MapId)
        {
            return;
        }

        var handle = args.WorldHandle;
        var (_, _, worldMatrix, invWorldMatrix) = _xform.GetWorldPositionRotationMatrixWithInv(gridXform);
        var tileSize = grid.TileSize;

        var query = _entManager.AllEntityQueryEnumerator<ESMapperComponent>();
        while (query.MoveNext(out var uid, out var mapper))
        {
            ESSelectionBox selection;
            float alpha;

            switch (mapper.SelectionState)
            {
                case ESSelectionState.Selecting { Selection: var liveSelection }:
                    selection = liveSelection;
                    alpha = 0.1f;
                    break;
                default:
                    if (mapper.ActiveGridSelection is not { } activeSelection)
                        continue;

                    selection = activeSelection;
                    alpha = 0.2f;
                    break;
            }

            var (mapId, worldBox) = _selection.ToWorldBox2Rotated(selection);
            if (mapId != args.MapId)
                continue;

            var localBox = invWorldMatrix.TransformBox(worldBox);

            var name = _entManager.GetComponent<MetaDataComponent>(uid).EntityName;
            var color = _chat.GetChatColor(name);

            handle.SetTransform(worldMatrix);

            foreach (var tileRef in _map.GetLocalTilesIntersecting(owner, grid, localBox))
            {
                var topLeft = new Vector2(tileRef.GridIndices.X, tileRef.GridIndices.Y) * tileSize;
                var center = topLeft + new Vector2(tileSize / 2f);

                if (!localBox.Contains(center))
                    continue;

                var tileBox = new Box2(topLeft, topLeft + new Vector2(tileSize, tileSize));
                handle.DrawRect(tileBox, color.WithAlpha(alpha));
            }
        }

        handle.SetTransform(Matrix3x2.Identity);
    }
}
