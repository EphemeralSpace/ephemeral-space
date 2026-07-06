using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

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

    /// <summary>
    ///     A panel will be hidden entirely if its calculated size would be below this value
    /// </summary>
    public float HidePanelsBelow { get; set; } = 100.0f;

    /// <summary>
    ///     A panel will be made visible (assuming its not already visible) if its calculated size would be above this value
    /// </summary>
    public float ShowPanelsAbove { get; set; } = 150.0f;

    private bool ShouldShowPanel(Control panel, float availableWidth)
    {
        if (panel.Visible)
            return availableWidth >= HidePanelsBelow;
        return availableWidth >= ShowPanelsAbove;
    }

    private (float left, float right) CalculatePanelWidths(float viewportWidth)
    {
        var panelSpace = Math.Max(0f, Size.X - viewportWidth);

        var rightPanelWidth = panelSpace / (PanelRatio + 1);
        var leftPanelWidth = panelSpace - rightPanelWidth;

        return (leftPanelWidth, rightPanelWidth);
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        if (ChildCount != 3)
            throw new ArgumentOutOfRangeException($"Child count of {nameof(HudViewportContainer)} must be exactly 3");

        Log.Info($"{IoCManager.Resolve<IGameTiming>().CurFrame} | hvc arranging size {finalSize}");

        var finalHeight = finalSize.Y;

        var leftPanel = GetChild(0);
        var centerContainer = GetChild(1);
        var rightPanel = GetChild(2);

        var viewportWidth = centerContainer.DesiredSize.X;
        var (leftPanelWidth, rightPanelWidth) = CalculatePanelWidths(viewportWidth);
        leftPanel.Visible = ShouldShowPanel(leftPanel, leftPanelWidth);
        rightPanel.Visible = ShouldShowPanel(rightPanel, rightPanelWidth);

        // arrange viewport first
        centerContainer.Arrange(UIBox2.FromDimensions(leftPanelWidth, 0, viewportWidth, finalHeight));

        // arrange panels around it
        if (leftPanel.Visible)
            leftPanel.Arrange(UIBox2.FromDimensions(0, 0, leftPanelWidth, finalHeight));
        if (rightPanel.Visible)
            rightPanel.Arrange(UIBox2.FromDimensions((leftPanel.Visible ? leftPanelWidth : 0) + viewportWidth, 0, rightPanelWidth, finalHeight));

        return finalSize;
    }
}
