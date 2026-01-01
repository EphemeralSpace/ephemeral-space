using Content.Shared._Offbrand.NuBody;
using Robust.Client.GameObjects;

namespace Content.Client._Offbrand.NuBody;

public sealed class OFMVisualBodySystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OFMVisualBodyComponent, OrganInsertedIntoEvent>(OnOrganInsertedInto);
        SubscribeLocalEvent<OFMVisualBodyComponent, OrganRemovedFromEvent>(OnOrganRemovedFrom);
    }

    private void OnOrganInsertedInto(Entity<OFMVisualBodyComponent> ent, ref OrganInsertedIntoEvent args)
    {
        if (!TryComp<OFMVisualOrganComponent>(args.Organ, out var visualOrgan))
            return;

        if (!_sprite.LayerMapTryGet(ent.Owner, visualOrgan.Layer, out var index, true))
            return;

        _sprite.LayerSetData(ent.Owner, index, visualOrgan.Data);
    }

    private void OnOrganRemovedFrom(Entity<OFMVisualBodyComponent> ent, ref OrganRemovedFromEvent args)
    {
        if (!TryComp<OFMVisualOrganComponent>(args.Organ, out var visualOrgan))
            return;

        if (!_sprite.LayerMapTryGet(ent.Owner, visualOrgan.Layer, out var index, true))
            return;

        _sprite.LayerSetVisible(ent.Owner, index, false);
    }
}
