using Content.Shared._ES.Cargo.Requests;
using Content.Shared._ES.Cargo.Requests.Components;
using Robust.Client.GameObjects;

namespace Content.Client._ES.Cargo.Requests;

public sealed class ESCargoRequestSystem : ESSharedCargoRequestSystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESCargoRequestStationComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleStateEvent);
    }

    private void OnAfterAutoHandleStateEvent(Entity<ESCargoRequestStationComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var query = EntityQueryEnumerator<ESCargoRequestConsoleComponent, UserInterfaceComponent>();
        while (query.MoveNext(out var uid, out var comp, out var ui))
        {
            if (_userInterface.TryGetOpenUi((uid, ui), ESCargoRequestConsoleUiKey.Key, out var bui))
                bui.Update();
        }
    }
}
