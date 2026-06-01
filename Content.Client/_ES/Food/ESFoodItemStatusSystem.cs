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

    private static readonly StyleBoxFlat StyleBoxUnlit = new()
    {
        BackgroundColor = Color.Black
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
            HorizontalAlignment = HAlignment.Center
        };

        AddChild(wrapper);

        for (var i = 0; i < parent.Comp.StartingPortions; i++)
        {
            var style = i <= ((_oldPortionsLeft ?? parent.Comp.StartingPortions) - 1) ? StyleBoxLit : StyleBoxUnlit;
            var panel = new PanelContainer { MinSize = new Vector2(20, 20), PanelOverride = style };
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
            var style = i <= ((_oldPortionsLeft ?? _parent.Comp.StartingPortions) - 1) ? StyleBoxLit : StyleBoxUnlit;
            var panel = _sections[i];
            panel.PanelOverride = style;
        }
    }
}
