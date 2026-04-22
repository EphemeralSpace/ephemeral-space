using System.Numerics;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._ES.Screens;

/// <summary>
///     Handles layouting for the hud UI with a left panel, viewport, and right panel.
///     All children will fill vertical space as much as possible.
///     The viewport will be laid out first
/// </summary>
public sealed class HudViewportContainer : Container
{
    /// <summary>
    ///     Determines how much space each panel gets compared to one another.
    ///     1 means each panel receives an equal amount of space.
    ///     2 would mean the left panel receives twice as much space as the right panel.
    ///     0.5 would mean the right panel receives twice as much space as the left panel.
    /// </summary>
    public float PanelRatio { get; set; } = 1.0f;

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        if (ChildCount != 3)
            throw new ArgumentOutOfRangeException($"Child count of {nameof(HudViewportContainer)} must be exactly 3");

        var (finalWidth, finalHeight) = finalSize;

        var leftPanel = GetChild(0);
        var centerContainer = GetChild(1);
        var rightPanel = GetChild(2);

        // lay out viewport in the center
        var viewportWidth = centerContainer.DesiredSize.X;
        var panelSpace = (finalWidth - viewportWidth);
        var rightPanelWidth = panelSpace / (PanelRatio + 1);
        var leftPanelWidth = panelSpace - rightPanelWidth;
        centerContainer.Arrange(UIBox2.FromDimensions(leftPanelWidth, 0, viewportWidth, finalHeight));

        // lay out panels
        leftPanel.Arrange(UIBox2.FromDimensions(0, 0, leftPanelWidth, finalHeight));
        rightPanel.Arrange(UIBox2.FromDimensions(leftPanelWidth + viewportWidth, 0, rightPanelWidth, finalHeight));

        return finalSize;
    }
}
