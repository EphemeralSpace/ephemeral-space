using Content.Server.DeviceLinking.Systems;
using Content.Shared._ES.Keypad;
using Content.Shared._ES.Keypad.Components;

namespace Content.Server._ES.Keypad;

/// <inheritdoc/>
public sealed partial class ESKeypadSystem : ESSharedKeypadSystem
{
    [Dependency] private DeviceLinkSystem _deviceLink = default!;

    protected override void OnLockToggled(Entity<ESKeypadComponent> ent)
    {
        var port = ent.Comp.Locked ? ent.Comp.LockPort: ent.Comp.UnlockPort;
        _deviceLink.InvokePort(ent, port);
    }
}
