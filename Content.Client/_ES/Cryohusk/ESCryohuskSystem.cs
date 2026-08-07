using Content.Shared._ES.Cryohusk;
using Content.Shared._ES.Cryohusk.Components;
using Robust.Client.GameObjects;

namespace Content.Client._ES.Cryohusk;

public sealed partial class ESCryohuskSystem : ESSharedCryohuskSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESCryohuskIdCardComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<ESCryohuskIdCardComponent> ent, ref ComponentStartup args)
    {
        if (_sprite.LayerExists(ent.Owner, ESCryohuskIdCardVisualLayers.Frost))
            return;

        _sprite.AddLayer(ent.Owner, ent.Comp.Overlay);
    }
}

public enum ESCryohuskIdCardVisualLayers : byte
{
    Frost,
}
