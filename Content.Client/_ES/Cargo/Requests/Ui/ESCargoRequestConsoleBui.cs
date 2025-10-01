using Content.Shared._ES.Cargo.Requests.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._ES.Cargo.Requests.Ui;

[UsedImplicitly]
public sealed class ESCargoRequestConsoleBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private ESCargoRequestConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ESCargoRequestConsoleWindow>();
        _window.Update(Owner);

        _window.OnDepartmentIdChanged += id => SendPredictedMessage(new ESSetDepartmentIdMessage(id));
        _window.OnCreateRequest += body => SendPredictedMessage(new ESCreateCargoRequestMessage(body));
        _window.OnStatusChanged += (rid, status) => SendPredictedMessage(new ESSetCargoRequestStatusMessage(rid, status));
    }

    public override void Update()
    {
        base.Update();

        _window?.Update(Owner);
    }
}
