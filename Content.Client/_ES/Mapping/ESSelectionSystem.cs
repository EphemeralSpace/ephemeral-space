using Content.Shared._ES.Mapping;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._ES.Mapping;

public sealed partial class ESSelectionSystem : ESSharedSelectionSystem
{
    public event Action<Entity<ESMapperComponent>>? OnMapperUpdate;
    public event Action? OnMapperDetach;

    [Dependency] private EntityQuery<ESMapperComponent> _mapper;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private SharedTransformSystem _xform = default!;

    private ESMapperSelectionOverlays _selectionOverlay = default!;
    private ESMapperSelectionTileOverlay _selectionTileOverlay = default!;
    private ESMapperEntitySelectionPreOverlay _selectionEntityPreOverlay = default!;
    private ESMapperEntitySelectionPostOverlay _selectionEntityPostOverlay = default!;

    [Access(typeof(ESMapperEntitySelectionPreOverlay), typeof(ESMapperEntitySelectionPostOverlay))]
    public List<(Entity<SpriteComponent> ent, Color baseColor)> CachedColors = new(128);

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder.Bind(EngineKeyFunctions.EditorPlaceObject,
                new PointerStateInputCmdHandler(OnMouseDown, OnMouseUp))
            .Register<ESSelectionSystem>();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnDetached);

        _selectionOverlay = new();
        _selectionTileOverlay = new();
        _selectionEntityPreOverlay = new();
        _selectionEntityPostOverlay = new();
    }

    private bool IsInBox(EntityCoordinates coordinates, ESSelectionBox? selBox)
    {
        if (selBox is null)
            return false;

        var (map, box) = ToWorldBox2Rotated(selBox.Value);
        var coordsMap = _xform.GetMapId(coordinates);
        var coordsWorld = _xform.ToWorldPosition(coordinates);
        return map == coordsMap && box.Contains(coordsWorld);
    }

    private bool OnMouseDown(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        if (!_mapper.TryComp(session?.AttachedEntity, out var mapper) || !TryComp<EyeComponent>(session.AttachedEntity, out var eye))
            return false;

        if (mapper.SelectionState is not ESSelectionState.Idle)
            return false;

        if (uid.IsValid() && mapper.ActiveEntitySelection?.Contains(uid) == true || IsInBox(coords, mapper.ActiveGridSelection))
        {
            RaisePredictiveEvent(new ESMappingStartMovingMessage(GetNetCoordinates(coords)));
            return true;
        }

        RaisePredictiveEvent(new ESMappingStartSelectionMessage(GetNetCoordinates(coords), eye.Rotation));
        return true;
    }

    private bool OnMouseUp(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        if (!_mapper.TryComp(session?.AttachedEntity, out var mapper))
            return false;

        if (mapper.SelectionState is ESSelectionState.Selecting)
        {
            RaisePredictiveEvent(new ESMappingStopSelectionMessage());
            return true;
        }
        if (mapper.SelectionState is ESSelectionState.Moving)
        {
            RaisePredictiveEvent(new ESMappingStopMovingMessage());
            return true;
        }

        return false;
    }

    protected override void OnStateUpdated(Entity<ESMapperComponent> ent)
    {
        if (ent.Owner != _player.LocalEntity)
            return;

        OnMapperUpdate?.Invoke(ent);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!_timing.IsFirstTimePredicted || !_input.MouseScreenPosition.IsValid)
            return;

        var player = _player.LocalEntity;

        if (player == null || !_mapper.TryComp(player, out var mapper))
            return;

        var coords = _input.MouseScreenPosition;
        var mousePos = _eye.PixelToMap(coords);

        if (mapper.SelectionState is ESSelectionState.Selecting { Selection: var selection })
        {
            var pos = _xform.ToMapCoordinates(new EntityCoordinates(selection.RelativeTo, selection.Start));
            if (mousePos.Position.EqualsApprox(pos.Position, 0.01d))
                return;

            if (mousePos.MapId == MapId.Nullspace)
                return;

            if (_timing.IsFirstTimePredicted)
                RaisePredictiveEvent(new ESMappingMidSelectionMessage(GetNetCoordinates(_xform.ToCoordinates(mousePos))));
            else
                RaiseLocalEvent(new ESMappingMidSelectionMessage(GetNetCoordinates(_xform.ToCoordinates(mousePos))));
        }
        else if (mapper.SelectionState is ESSelectionState.Moving)
        {
            if (_timing.IsFirstTimePredicted)
                RaisePredictiveEvent(new ESMappingMidMovingMessage(GetNetCoordinates(_xform.ToCoordinates(mousePos))));
            else
                RaiseLocalEvent(new ESMappingMidMovingMessage(GetNetCoordinates(_xform.ToCoordinates(mousePos))));
        }
    }

    private void OnAttached(LocalPlayerAttachedEvent ev)
    {
        if (_mapper.TryComp(ev.Entity, out var mapper))
        {
            OnMapperUpdate?.Invoke((ev.Entity, mapper));
            AddOverlays();
        }
        else
        {
            OnMapperDetach?.Invoke();
            RemoveOverlays();
        }
    }

    private void OnDetached(LocalPlayerDetachedEvent ev)
    {
        OnMapperDetach?.Invoke();
        RemoveOverlays();
    }

    private void AddOverlays()
    {
        _overlay.AddOverlay(_selectionOverlay);
        _overlay.AddOverlay(_selectionTileOverlay);
        _overlay.AddOverlay(_selectionEntityPreOverlay);
        _overlay.AddOverlay(_selectionEntityPostOverlay);
    }

    private void RemoveOverlays()
    {
        _overlay.RemoveOverlay(_selectionOverlay);
        _overlay.RemoveOverlay(_selectionTileOverlay);
        _overlay.RemoveOverlay(_selectionEntityPreOverlay);
        _overlay.RemoveOverlay(_selectionEntityPostOverlay);
    }
}
