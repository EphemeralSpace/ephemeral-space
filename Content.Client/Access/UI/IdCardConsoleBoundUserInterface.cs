using Content.Shared.Access.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.CrewManifest;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using static Content.Shared.Access.Components.IdCardConsoleComponent;

namespace Content.Client.Access.UI;

[UsedImplicitly]
public sealed partial class IdCardConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private IdCardConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<IdCardConsoleWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

        _window.OnSubmitData += (newFullName, newJobTitle, newJobPrototype) =>
        {
            SendMessage(new WriteToTargetIdMessage(
                newFullName,
                newJobTitle,
                newJobPrototype));
        };

        _window.CrewManifestButton.OnPressed += _ => SendMessage(new CrewManifestOpenUiMessage());
        _window.TargetIdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(TargetIdCardSlotId));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        var castState = (IdCardConsoleBoundUserInterfaceState) state;
        _window?.UpdateState(castState);
    }
}
