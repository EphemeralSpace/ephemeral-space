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
    ///     A panel will be hidden entirely if its calculated pixel size would be below this value
    /// </summary>
    public float HidePanelsBelow { get; set; } = 140.0f;

    /// <summary>
    ///     A panel will be made visible (assuming its not already visible) if its calculated pixel size would be above this value
    /// </summary>
    public float ShowPanelsAbove { get; set; } = 160.0f;

    private bool _leftVisible = false;
    private bool _rightVisible = false;

    private bool ShouldShowPanel(Control panel, float availableWidth)
    {
        if (panel.Visible)
            return (availableWidth * UIScale) >= HidePanelsBelow;
        return (availableWidth * UIScale) >= ShowPanelsAbove;
    }

    private (float left, float right) CalculatePanelWidths(float totalWidth, float viewportWidth)
    {
        var panelSpace = Math.Max(0f, totalWidth - viewportWidth);

        var rightPanelWidth = panelSpace / (PanelRatio + 1);
        var leftPanelWidth = panelSpace - rightPanelWidth;

        return (leftPanelWidth, rightPanelWidth);
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        if (ChildCount != 3)
            throw new ArgumentOutOfRangeException($"Child count of {nameof(HudViewportContainer)} must be exactly 3");

        var leftPanel = GetChild(0);
        var centerContainer = GetChild(1);
        var rightPanel = GetChild(2);

        centerContainer.Measure(availableSize);
        var (leftPanelWidth, rightPanelWidth) = CalculatePanelWidths(availableSize.X, centerContainer.DesiredSize.X);
        _leftVisible = ShouldShowPanel(leftPanel, leftPanelWidth);
        _rightVisible = ShouldShowPanel(rightPanel, rightPanelWidth);
        leftPanel.Visible = _leftVisible;
        rightPanel.Visible = _rightVisible;

        if (_leftVisible)
            leftPanel.Measure(availableSize with { X = leftPanelWidth });
        if (_rightVisible)
            rightPanel.Measure(availableSize with { X = rightPanelWidth });

        return availableSize;
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        if (ChildCount != 3)
            throw new ArgumentOutOfRangeException($"Child count of {nameof(HudViewportContainer)} must be exactly 3");

        var finalHeight = finalSize.Y;

        var leftPanel = GetChild(0);
        var centerContainer = GetChild(1);
        var rightPanel = GetChild(2);

        var viewportWidth = centerContainer.DesiredSize.X;
        var (leftPanelWidth, rightPanelWidth) = CalculatePanelWidths(finalSize.X, viewportWidth);

        // arrange viewport first
        centerContainer.Arrange(UIBox2.FromDimensions(leftPanelWidth, 0, viewportWidth, finalHeight));

        // arrange panels around it
        if (_leftVisible)
            leftPanel.Arrange(UIBox2.FromDimensions(0, 0, leftPanelWidth, finalHeight));
        if (_rightVisible)
            rightPanel.Arrange(UIBox2.FromDimensions(leftPanelWidth + viewportWidth, 0, rightPanelWidth, finalHeight));

        return finalSize;
    }
}
