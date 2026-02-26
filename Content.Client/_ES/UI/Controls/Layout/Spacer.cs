using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._ES.UI.Controls.Layout;

/// <summary>
///     A spacer for use within box layouts.
/// </summary>
public sealed class Spacer : Control
{
    public float VSpace
    {
        get => MinHeight;
        set
        {
            MinHeight = value;
            if (Parent is not BoxContainer b)
                return;

            if (b.Orientation is not BoxContainer.LayoutOrientation.Horizontal)
                Log.Warning("You have a vertical spacer in a horizontal stack layout, this probably isn't what you want.");
        }
    }

    public float HSpace
    {
        get => MinWidth;
        set
        {
            MinWidth = value;

            if (Parent is not BoxContainer b)
                return;

            if (b.Orientation is not BoxContainer.LayoutOrientation.Vertical)
                Log.Warning("You have a horizontal spacer in a vertical stack layout, this probably isn't what you want.");


        }
    }
}

