using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._ES.Cargo.Storeroom.Ui;

[UsedImplicitly]
public sealed class ESWarehouseManifestBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private ESWarehouseManifestWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ESWarehouseManifestWindow>();
        _window.OpenCentered();
        _window.Update(Owner);
    }

    public override void Update()
    {
        base.Update();
        _window?.Update(Owner);
    }
}
