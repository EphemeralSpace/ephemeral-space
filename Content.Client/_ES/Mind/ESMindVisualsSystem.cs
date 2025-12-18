using Content.Shared._ES.Mind;
using Content.Shared._ES.Mind.Components;
using Robust.Client.GameObjects;

namespace Content.Client._ES.Mind;

public sealed class ESMindVisualsSystem : ESSharedMindVisualsSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESMindVisualsComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<ESMindVisualsComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!Appearance.TryGetData<bool>(ent, ESMindVisuals.HasMind, out var hasMind))
            hasMind = true;

        if (!Appearance.TryGetData<bool>(ent, ESMindVisuals.InGame, out var inGame))
            inGame = true;

        _sprite.SetVisible(ent.Owner, inGame);
        _sprite.SetColor(ent.Owner, hasMind ? Color.White : ent.Comp.NoMindColor);
    }
}
