using Content.Shared._ES.Cargo.Requests;
using Content.Shared._ES.Cargo.Requests.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client._ES.Cargo.Requests;

public sealed class ESCargoRequestSystem : ESSharedCargoRequestSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESCargoRequestStationComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleStateEvent);

        SubscribeLocalEvent<ESCargoRequestConsoleComponent, AfterAutoHandleStateEvent>(OnConsoleAfterHandleStateEvent);
        SubscribeLocalEvent<ESCargoRequestConsoleComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    protected override void OnSetDepartmentId(Entity<ESCargoRequestConsoleComponent> ent, ref ESSetDepartmentIdMessage args)
    {
        base.OnSetDepartmentId(ent, ref args);

        if (!_timing.ApplyingState)
            return;

        if (_userInterface.TryGetOpenUi(ent.Owner, ESCargoRequestConsoleUiKey.Key, out var bui))
            bui.Update();
    }

    private void OnAfterAutoHandleStateEvent(Entity<ESCargoRequestStationComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!_timing.ApplyingState)
            return;

        var query = EntityQueryEnumerator<ESCargoRequestConsoleComponent, UserInterfaceComponent>();
        while (query.MoveNext(out var uid, out _, out var ui))
        {
            if (_userInterface.TryGetOpenUi((uid, ui), ESCargoRequestConsoleUiKey.Key, out var bui))
                bui.Update();
        }
    }

    private void OnConsoleAfterHandleStateEvent(Entity<ESCargoRequestConsoleComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!_timing.ApplyingState)
            return;

        if (_userInterface.TryGetOpenUi(ent.Owner, ESCargoRequestConsoleUiKey.Key, out var bui))
            bui.Update();
    }

    private void OnAppearanceChange(Entity<ESCargoRequestConsoleComponent> ent, ref AppearanceChangeEvent args)
    {
        if (Appearance.TryGetData<bool>(ent, ESCargoRequestConsoleVisuals.Update, out var update, args.Component))
        {
            // Hardcoded appearance? More likely than you think.
            var state = $"request-{(update ? "1" : "0")}";
            _sprite.LayerSetRsiState((ent, args.Sprite), "computerLayerScreen", state);
        }
    }
}
