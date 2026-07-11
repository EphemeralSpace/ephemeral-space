using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Charges.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Charges;

public sealed class ChargesStatusControl : Control
{
    private readonly Entity<LimitedChargesComponent> _parent;
    private readonly RichTextLabel _label;

    public ChargesStatusControl(Entity<LimitedChargesComponent> parent)
    {
        _parent = parent;
        _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
        AddChild(_label);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_parent.Comp.LifeStage > ComponentLifeStage.Running)
        {
            _label.Visible = false;
            return;
        }

        _label.SetMarkup(Loc.GetString("limited-charges-charge-status-control-label",
            ("charges", _parent.Comp.LastCharges)));
    }
}
