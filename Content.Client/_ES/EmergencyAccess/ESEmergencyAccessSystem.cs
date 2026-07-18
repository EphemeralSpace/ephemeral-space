using Content.Shared._ES.EmergencyAccess;
using Content.Shared._ES.EmergencyAccess.Components;
using Robust.Client.GameObjects;

namespace Content.Client._ES.EmergencyAccess;

public sealed partial class ESEmergencyAccessSystem : ESSharedEmergencyAccessSystem
{
    [Dependency] private UserInterfaceSystem _userInterface = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESEmergencyAccessConsoleComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnAfterAutoHandleState(Entity<ESEmergencyAccessConsoleComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_userInterface.TryGetOpenUi(ent.Owner, ESEmergencyAccessConsoleUiKey.Key, out var ui))
            ui.Update();
    }
}
