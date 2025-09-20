using Content.Shared._ES.Cargo.Storeroom;
using Content.Shared._ES.Cargo.Storeroom.Components;
using Robust.Client.GameObjects;

namespace Content.Client._ES.Cargo.Storeroom;

public sealed class ESStoreroomSystem : ESSharedStoreroomSystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESStoreroomStationComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnAfterAutoHandleState(Entity<ESStoreroomStationComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var query = EntityQueryEnumerator<ESCargoManifestComputerComponent, UserInterfaceComponent>();
        while (query.MoveNext(out var uid, out _, out var ui))
        {
            if (_userInterface.TryGetOpenUi((uid, ui), ESCargoManifestUiKey.Key, out var bui))
                bui.Update();
        }
    }
}
