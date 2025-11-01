using Robust.Client.GameObjects;

namespace Content.Client._ES.Wallmount.Posters;

/// <inheritdoc cref="ESPosterComponent"/>
public sealed class ESPosterSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESPosterComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<ESPosterComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        _sprite.LayerSetRsiState((ent, sprite), ESPosterVisuals.Base, ent.Comp.State);
    }
}
