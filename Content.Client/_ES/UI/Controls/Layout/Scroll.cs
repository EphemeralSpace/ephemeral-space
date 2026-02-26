using Robust.Client.UserInterface.Controls;

namespace Content.Client._ES.UI.Controls.Layout;

/// <summary>
/// <para>
///     A container that provides a scroll bar for its contents, allowing its contents to be navigated horizontally
///     and/or vertically.
/// </para>
/// </summary>
/// <remarks>
/// <para>
///     Due to limitations of RT's UI system, you shouldn't use AddChild and co on this control.
///     A stack is automatically created within the Scroll and can be accessed through <see cref="Inner"/>. New
///     elements should be added to that child control, instead.
/// </para>
/// <para>
///     This remark is not relevant to XAML-created Scrolls, as <see cref="Inner"/> is where this element's
///     XAML children are added.
/// </para>
/// </remarks>
[Virtual]
public class Scroll : ScrollContainer
{
    public Stack Inner { get; }

    public Scroll()
    {
        Inner = new();
        AddChild(Inner);

        XamlChildren = Inner.Children;
    }
}

/// <summary>
///     A <see cref="Scroll"/> that fills its container on all axis, with both horizontal and vertical scroll support.
/// </summary>
[Virtual]
public class FillScroll : Scroll
{
    public FillScroll()
    {
        HorizontalExpand = true;
        VerticalExpand = true;

        HScrollEnabled = true;
        VScrollEnabled = true;
    }
}

/// <summary>
///     A <see cref="Scroll"/> that only supports vertical scrolling, and is sized to fit its contents horizontally
///     while filling its parent vertically.
/// </summary>
[Virtual]
public class VScroll : Scroll
{
    public VScroll()
    {
        HScrollEnabled = false;
        VScrollEnabled = true;

        VerticalExpand = true;

        Inner.Orientation = BoxContainer.LayoutOrientation.Vertical;
    }
}

/// <summary>
///     A <see cref="Scroll"/> that only supports horizontal scrolling, and is sized to fit its contents vertically
///     while filling its parent horizontally.
/// </summary>
[Virtual]
public class HScroll : Scroll
{
    public HScroll()
    {
        HScrollEnabled = true;
        VScrollEnabled = false;

        HorizontalExpand = true;

        Inner.Orientation = BoxContainer.LayoutOrientation.Horizontal;
    }
}
