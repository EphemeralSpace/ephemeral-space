using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._ES.Keypad.Ui;

[UsedImplicitly]
public sealed class ESKeypadBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private ESKeypadWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ESKeypadWindow>();
        _window.OpenCentered();
        _window.SetEntity(Owner);
    }
}

