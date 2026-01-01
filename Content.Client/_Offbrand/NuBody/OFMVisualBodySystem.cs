using Content.Shared._Offbrand.NuBody;
using Content.Shared.Humanoid.Markings;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Utility;

namespace Content.Client._Offbrand.NuBody;

public sealed class OFMVisualBodySystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly MarkingManager _marking = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OFMVisualOrganComponent, OrganGotInsertedEvent>(OnOrganGotInserted);
        SubscribeLocalEvent<OFMVisualOrganComponent, OrganGotRemovedEvent>(OnOrganGotRemoved);
        SubscribeLocalEvent<OFMVisualOrganComponent, AfterAutoHandleStateEvent>(OnOrganState);

        SubscribeLocalEvent<OFMVisualOrganMarkingsComponent, OrganGotInsertedEvent>(OnMarkingsGotInserted);
        SubscribeLocalEvent<OFMVisualOrganMarkingsComponent, OrganGotRemovedEvent>(OnMarkingsGotRemoved);
        SubscribeLocalEvent<OFMVisualOrganMarkingsComponent, AfterAutoHandleStateEvent>(OnMarkingsState);
    }

    private void OnOrganGotInserted(Entity<OFMVisualOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        ApplyVisual(ent, args.Target);
    }

    private void OnOrganGotRemoved(Entity<OFMVisualOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        RemoveVisual(ent, args.Target);
    }

    private void OnOrganState(Entity<OFMVisualOrganComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (Comp<OFMOrganComponent>(ent).Body is not { } body)
            return;

        ApplyVisual(ent, body);
    }

    private void ApplyVisual(Entity<OFMVisualOrganComponent> ent, EntityUid target)
    {
        if (!_sprite.LayerMapTryGet(target, ent.Comp.Layer, out var index, true))
            return;

        _sprite.LayerSetData(target, index, ent.Comp.Data);
    }

    private void RemoveVisual(Entity<OFMVisualOrganComponent> ent, EntityUid target)
    {
        if (!_sprite.LayerMapTryGet(target, ent.Comp.Layer, out var index, true))
            return;

        _sprite.LayerSetRsiState(target, index, RSI.StateId.Invalid);
    }

    private void OnMarkingsGotInserted(Entity<OFMVisualOrganMarkingsComponent> ent, ref OrganGotInsertedEvent args)
    {
        ApplyMarkings(ent, args.Target);
    }

    private void OnMarkingsGotRemoved(Entity<OFMVisualOrganMarkingsComponent> ent, ref OrganGotRemovedEvent args)
    {
        RemoveMarkings(ent, args.Target);
    }

    private void OnMarkingsState(Entity<OFMVisualOrganMarkingsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (Comp<OFMOrganComponent>(ent).Body is not { } body)
            return;

        RemoveMarkings(ent, body);
        ApplyMarkings(ent, body);
    }

    private void ApplyMarkings(Entity<OFMVisualOrganMarkingsComponent> ent, EntityUid target)
    {
        foreach (var marking in ent.Comp.Markings)
        {
            if (!_marking.TryGetMarking(marking, out var proto))
                continue;

            if (!_sprite.LayerMapTryGet(target, proto.BodyPart, out var index, true))
                continue;

            for (var i = 0; i < proto.Sprites.Count; i++)
            {
                var sprite = proto.Sprites[i];

                DebugTools.Assert(sprite is SpriteSpecifier.Rsi);
                if (sprite is not SpriteSpecifier.Rsi rsi)
                    continue;

                var layerID = $"{proto.ID}-{rsi.RsiState}";

                if (!_sprite.LayerMapTryGet(target, layerID, out _, false))
                {
                    var layer = _sprite.AddLayer(target, sprite, index + i + 1);
                    _sprite.LayerMapSet(target, layerID, layer);
                    _sprite.LayerSetSprite(target, layerID, rsi);
                }

                if (marking.MarkingColors is not null && i < marking.MarkingColors.Count)
                    _sprite.LayerSetColor(target, layerID, marking.MarkingColors[i]);
                else
                    _sprite.LayerSetColor(target, layerID, Color.White);
            }
        }
        ent.Comp.AppliedMarkings = ent.Comp.Markings;
    }

    private void RemoveMarkings(Entity<OFMVisualOrganMarkingsComponent> ent, EntityUid target)
    {
        foreach (var marking in ent.Comp.AppliedMarkings)
        {
            if (!_marking.TryGetMarking(marking, out var proto))
                continue;

            foreach (var sprite in proto.Sprites)
            {
                DebugTools.Assert(sprite is SpriteSpecifier.Rsi);
                if (sprite is not SpriteSpecifier.Rsi rsi)
                    continue;

                var layerID = $"{proto.ID}-{rsi.RsiState}";

                if (!_sprite.LayerMapTryGet(target, layerID, out var index, false))
                    continue;

                _sprite.LayerMapRemove(target, layerID);
                _sprite.RemoveLayer(target, index);
            }
        }
    }
}
