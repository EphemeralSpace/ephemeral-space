using Content.Client._ES.Evac.Ui;
using Content.Shared._ES.Evac;
using Content.Shared._ES.Evac.Components;
using Robust.Client.GameObjects;

namespace Content.Client._ES.Evac;

/// <inheritdoc/>
public sealed class ESEvacSystem : ESSharedEvacSystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESEvacStationComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
    }

    private void OnAfterHandleState(Entity<ESEvacStationComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var query = EntityQueryEnumerator<ESEvacConsoleComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (!_userInterface.TryGetOpenUi(uid, ESEvacUiKey.Key, out var ui) ||
                ui is not ESEvacBeaconBui bui)
                continue;
            bui.Update();
        }
    }
}
