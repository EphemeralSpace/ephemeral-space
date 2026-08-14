using Content.Shared._ES.Chat;
using Content.Shared._ES.Mapping;
using Robust.Client.ComponentTrees;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client._ES.Mapping;

public sealed partial class ESMapperEntitySelectionPreOverlay : Overlay
{
    [Dependency] private IEntityManager _entManager = default!;

    private readonly ESSharedChatSystem _chat;
    private readonly SpriteTreeSystem _spriteTree;
    private readonly SpriteSystem _sprite;
    private readonly ESSelectionSystem _selection;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    public ESMapperEntitySelectionPreOverlay()
    {
        IoCManager.InjectDependencies(this);
        _chat = _entManager.System<ESSharedChatSystem>();
        _spriteTree = _entManager.System<SpriteTreeSystem>();
        _sprite = _entManager.System<SpriteSystem>();
        _selection = _entManager.System<ESSelectionSystem>();
    }

    private readonly Dictionary<EntityUid, Color> _tints = new();
    private void UpdateTints()
    {
        _tints.Clear();

        var query = _entManager.AllEntityQueryEnumerator<ESMapperComponent>();

        while (query.MoveNext(out var uid, out var mapper))
        {
            HashSet<EntityUid> entities;

            switch (mapper.SelectionState)
            {
                case ESSelectionState.Selecting { Selection: var liveSelection }:
                    entities = _selection.SelectEntities(liveSelection);
                    break;

                default:
                    if (mapper.ActiveEntitySelection is not { } activeSelection)
                        continue;

                    entities = activeSelection;
                    break;
            }

            var name = _entManager.GetComponent<MetaDataComponent>(uid).EntityName;
            var color = _chat.GetChatColor(name);

            foreach (var entity in entities)
            {
                if (_tints.TryGetValue(entity, out var existingColor))
                {
                    _tints[entity] = new Color(
                        Math.Max(existingColor.R, color.R),
                        Math.Max(existingColor.G, color.G),
                        Math.Max(existingColor.B, color.B),
                        Math.Max(existingColor.A, color.A));
                }
                else
                {
                    _tints[entity] = color;
                }
            }
        }
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var sprites = _spriteTree.QueryAabb(args.MapId, args.WorldBounds);
        UpdateTints();

        foreach (var sprite in sprites)
        {
            if (!_tints.TryGetValue(sprite.Uid, out var tint))
                continue;

            var oldColor = sprite.Component.Color;
            _sprite.SetColor((sprite.Uid, sprite.Component), tint);
            _selection.CachedColors.Add(((sprite.Uid, sprite.Component), oldColor));
        }
    }
}

public sealed partial class ESMapperEntitySelectionPostOverlay : Overlay
{
    [Dependency] private IEntityManager _ent = default!;
    private readonly ESSelectionSystem _selection;
    private readonly SpriteSystem _sprite;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public ESMapperEntitySelectionPostOverlay()
    {
        IoCManager.InjectDependencies(this);

        _selection = _ent.EntitySysManager.GetEntitySystem<ESSelectionSystem>();
        _sprite = _ent.EntitySysManager.GetEntitySystem<SpriteSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        foreach (var (ent, oldColor) in _selection.CachedColors)
        {
            _sprite.SetColor(ent!, oldColor);
        }

        _selection.CachedColors.Clear();
    }
}
