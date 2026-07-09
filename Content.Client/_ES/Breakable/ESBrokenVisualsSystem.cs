using Content.Client._ES.Breakable.Components;
using Content.Shared._ES.Breakable.Components;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client._ES.Breakable;

public sealed partial class ESBrokenVisualsSystem : EntitySystem
{
    [Dependency] private IResourceCache _resourceCache = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESBrokenVisualsComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnAppearanceChanged(Entity<ESBrokenVisualsComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!_appearance.TryGetData(ent, ESBreakableVisuals.Broken, out bool broken))
            return;

        var sprite = broken ? ent.Comp.BrokenRSI : ent.Comp.BaseRSI;

        if (_resourceCache.TryGetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / sprite, out var rsi))
        {
            _sprite.SetBaseRsi(ent.Owner, rsi.RSI);
        }
    }
}
