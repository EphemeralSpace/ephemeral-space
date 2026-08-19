using Content.Client._ES.Breakable.Components;
using Content.Shared._ES.Breakable.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._ES.Breakable;

public sealed partial class ESBreakableVisualsSystem : EntitySystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESBreakableVisualsComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ESBreakableVisualsComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnStartup(Entity<ESBreakableVisualsComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        foreach (var baseKey in ent.Comp.BrokenLayers.Keys)
        {
            if (!_sprite.TryGetLayer((ent, sprite), baseKey, out var layer, true))
                continue;

            ent.Comp.BaseLayers[baseKey] =
                new SpriteSpecifier.Rsi(layer.ActualRsi?.Path ?? new ResPath(), layer.State.Name ?? string.Empty);
        }
    }

    private void OnAppearanceChange(Entity<ESBreakableVisualsComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (!_appearance.TryGetData<bool>(ent.Owner, ESBreakableVisuals.Broken, out var broken))
            return;

        var layers = broken ? ent.Comp.BrokenLayers : ent.Comp.BaseLayers;
        foreach (var (key, spriteSpecifier) in layers)
        {
            _sprite.LayerSetSprite((ent, sprite), key, spriteSpecifier);
        }
    }
}
