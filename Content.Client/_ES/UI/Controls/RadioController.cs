using System.Diagnostics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._ES.UI.Controls;

// In a better universe this would not be a control.
public sealed class RadioController : Control
{
    public ButtonGroup Group { get; set; } = new();

    public List<string>? Buttons { get; set; } = null;

    public string? ButtonHolder { get; set; } = null;

    public bool FromRelatives { get; set; } = false;

    protected override void EnteredTree()
    {
        base.EnteredTree();

        var buttons = new List<BaseButton>();

        var scope = FindNameScope();


        if (FromRelatives)
        {
            // Find our relatives.
            foreach (var relative in Parent!.Children)
            {
                if (ReferenceEquals(relative, this))
                    continue;

                buttons.Add((BaseButton)relative);
            }
        }
        else if (ButtonHolder is { } buttonHolder)
        {
            if (scope is null)
                throw new NotSupportedException($"Can't use {nameof(ButtonHolder)} in a context with no NameScope.");

            if (scope.Find(buttonHolder) is not {} holder)
                throw new KeyNotFoundException($"Could not find {buttonHolder}.");

            foreach (var child in holder.Children)
            {
                buttons.Add((BaseButton)child);
            }
        }
        else if (Buttons is {} buttonNames)
        {
            if (scope is null)
                throw new NotSupportedException($"Can't use {nameof(ButtonHolder)} in a context with no NameScope.");

            foreach (var buttonName in buttonNames)
            {
                if (scope.Find(buttonName) is not {} button)
                    throw new KeyNotFoundException($"Could not find {buttonName}.");

                buttons.Add((BaseButton)button);
            }
        }

        foreach (var button in buttons)
        {
            button.Group = Group;
        }
    }
}
