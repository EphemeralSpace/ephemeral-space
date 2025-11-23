using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._ES.Spawning.Ui;

[UsedImplicitly]
public sealed class ESSpawningBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [UsedImplicitly]
    private ESSpawningWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ESSpawningWindow>();
    }
}
