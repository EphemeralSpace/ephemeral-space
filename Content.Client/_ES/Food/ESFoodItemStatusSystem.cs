using System.Numerics;
using Content.Client.Items;
using Content.Shared._ES.Food;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._ES.Food;

/// <summary>
///     Handles generating the item status control for <see cref="ESFoodComponent"/>.
/// </summary>
public sealed class ESFoodItemStatusSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<ESFoodComponent>(ent => new ESFoodStatusControl(ent));
    }
}

public sealed class ESFoodStatusControl : Control
{
    private readonly Entity<ESFoodComponent> _parent;
    private readonly List<PanelContainer> _sections;
    private int? _oldPortionsLeft;

    private static readonly StyleBoxFlat StyleBoxLit = new()
    {
        BackgroundColor = Color.LimeGreen
    };

    private static readonly StyleBoxFlat StyleBoxBad = new()
    {
        BackgroundColor = Color.Red
    };

    public ESFoodStatusControl(Entity<ESFoodComponent> parent)
    {
        _parent = parent;
        _sections = new();
        _oldPortionsLeft = parent.Comp.PortionsLeft;

        var wrapper = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 4,
            HorizontalAlignment = HAlignment.Left,
            Margin = new Thickness(5)
        };

        AddChild(wrapper);

        // always create 5 sections, even if there arent that many portions
        for (var i = 0; i < 5; i++)
        {
            var colorBox = parent.Comp.SatietyMultiplier < 0 ? StyleBoxBad : StyleBoxLit;
            var visible = i <= ((_oldPortionsLeft ?? parent.Comp.StartingPortions) - 1);
            var panel = new PanelContainer { MinSize = new Vector2(16, 16), PanelOverride = colorBox, Visible = visible};
            wrapper.AddChild(panel);
            _sections.Add(panel);
        }
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_parent.Comp.PortionsLeft == _oldPortionsLeft)
            return;

        _oldPortionsLeft = _parent.Comp.PortionsLeft;

        for (var i = 0; i < _sections.Count; i++)
        {
            var colorBox = _parent.Comp.SatietyMultiplier < 0 ? StyleBoxBad : StyleBoxLit;
            var visible = i <= ((_oldPortionsLeft ?? _parent.Comp.StartingPortions) - 1);
            var panel = _sections[i];
            panel.PanelOverride = colorBox;
            panel.Visible = visible;
        }
    }
}
