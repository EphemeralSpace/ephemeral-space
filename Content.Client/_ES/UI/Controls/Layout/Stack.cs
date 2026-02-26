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
public class VFillStack : VStack
{
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
public class HFillStack : HStack
{
    public HFillStack()
    {
        VerticalExpand = true;
    }
}
