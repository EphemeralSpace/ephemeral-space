using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._ES.UI.Controls.Layout;

/// <summary>
///     A container for a "stack" of controls, with an <see cref="Stack.Orientation">orientation</see> and
///     <see cref="Stack.Align">alignment along it</see>.
/// </summary>
/// <seealso cref="VStack"/>
/// <seealso cref="HStack"/>
/// <seealso cref="VFillStack"/>
/// <seealso cref="HFillStack"/>
[Virtual]
public class Stack : BoxContainer
{

    [UsedImplicitly] // XAMLIL
    public static ExpansionAxis GetExpandAxis(Control c)
    {
        var horizontal = c.HorizontalExpand ? ExpansionAxis.Horizontal : ExpansionAxis.None;
        var vertical = c.VerticalExpand ? ExpansionAxis.Vertical : ExpansionAxis.None;

        if (c is IImplicitExpansionControl i)
            throw new Exception($"Cannot use the Expansion property on controls that imply their own expansion. Was using {i.GetType()}, you probably want {i.PreferredType}");

        return horizontal | vertical;
    }

    [UsedImplicitly] // XAMLIL
    public static void SetExpandAxis(Control c, ExpansionAxis axis)
    {
        c.HorizontalExpand = (axis & ExpansionAxis.Horizontal) != 0;
        c.VerticalExpand = (axis & ExpansionAxis.Vertical) != 0;
    }
}

[Flags]
public enum ExpansionAxis
{
    None = 0,
    Horizontal = 1,
    Vertical = 2,
    Both = Horizontal | Vertical,
}


/// <summary>
///     A vertically oriented container for a "stack" of controls.
///     This only grows as necessary on the horizontal and vertical axis by default.
/// </summary>
/// <seealso cref="Stack"/>
[Virtual]
public class VStack : Stack
{
    public VStack()
    {
        Orientation = LayoutOrientation.Vertical;
    }
}

/// <summary>
///     A vertically oriented container for a "stack" of controls, which also fills horizontal space.
///     This always fills its container on the horizontal axis, while only growing as necessary vertically by default.
/// </summary>
/// <seealso cref="Stack"/>
[Virtual]
public class VFillStack : VStack, IImplicitExpansionControl
{
    public Type PreferredType => typeof(VStack);

    public VFillStack()
    {
        HorizontalExpand = true;
    }
}

/// <summary>
///     A horizontally oriented container for a "stack" of controls.
///     This only grows as necessary on the horizontal and vertical axis by default.
/// </summary>
/// <seealso cref="Stack"/>
[Virtual]
public class HStack : Stack
{
    public HStack()
    {
        Orientation = LayoutOrientation.Horizontal;
    }
}

/// <summary>
///     A horizontally oriented container for a "stack" of controls, which also fills vertical space.
///     This always fills its container on the vertical axis, while only growing as necessary horizontally by default.
/// </summary>
/// <seealso cref="Stack"/>
[Virtual]
public class HFillStack : HStack, IImplicitExpansionControl
{
    public Type PreferredType => typeof(HStack);

    public HFillStack()
    {
        VerticalExpand = true;
    }
}
