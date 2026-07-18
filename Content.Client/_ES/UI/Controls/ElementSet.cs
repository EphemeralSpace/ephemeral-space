using System.Linq;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._ES.UI.Controls;

using AttachedButtonData = (BaseButton Button, Action<BaseButton.ButtonEventArgs> Action)?;

/// <summary>
///     A container that manages a set of children and decides which to draw.
/// </summary>
public sealed class ElementSet : Control
{
    public static readonly AttachedProperty<string?> SelectedBy =
        AttachedProperty<string?>.CreateNull("SelectedBy", typeof(ElementSet));

    private static readonly AttachedProperty<AttachedButtonData> AttachedButton =
        AttachedProperty<AttachedButtonData>.Create("AttachedButton", typeof(ElementSet));

    public Control CurrentElement { get; private set; } = default!;

    [UsedImplicitly] // XAMLIL
    public static string? GetSelectedBy(Control c)
        => c.GetValue(SelectedBy);

    // TODO: Support more selected-by setups than just explicitly named radio buttons.
    [UsedImplicitly] // XAMLIL
    public static void SetSelectedBy(Control c, string? selector)
    {
        c.SetValue(SelectedBy, selector);

        if (c.Parent is null)
            return;

        c.Parent.InvalidateArrange();

        if (c.Parent is not ElementSet set)
            throw new Exception($"The {SelectedBy} attached property is only usable with ElementSets.");

        set.ProcessSelectedBy(c);
    }

    private void ProcessSelectedBy(Control c)
    {
        if (!IsInsideTree)
            return; // Still too early. EnteredTree will get to it.

        var selector = c.GetValue(SelectedBy);

        if (c.GetValue(AttachedButton) is { } b)
        {
            // Unsubscribe from updates.
            b.Button.OnToggled -= b.Action;
        }

        if (selector is null)
            return; // Done.

        var namescope = c.FindNameScope();

        if (namescope?.Find(selector) is not BaseButton button)
            throw new Exception($"Could not find {selector} when setting {SelectedBy} for {c}");

        var action = (BaseButton.ButtonEventArgs args) =>
        {
            SetVisibleElement(c);
        };

        button.OnPressed += action;

        if (button.Pressed)
            SetVisibleElement(c);

        c.SetValue<AttachedButtonData>(AttachedButton, (button, action));
    }

    public void SetVisibleElement(Control c)
    {
        DebugTools.Assert(Children.Contains(c));

        foreach (var child in Children)
        {
            child.Visible = false; // Begone.
        }

        c.Visible = true;
        CurrentElement = c;

        if (c.GetValue<AttachedButtonData>(AttachedButton) is { } data)
        {
            // We're part of a group so go back and set the button as pressed.
            if (data.Button.Group is not null)
                data.Button.Pressed = true;
        }
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        foreach (var c in Children)
        {
            ProcessSelectedBy(c);
        }

        SetVisibleElement(Children.First());
    }

    protected override void ChildAdded(Control newChild)
    {
        ProcessSelectedBy(newChild);
    }
}
