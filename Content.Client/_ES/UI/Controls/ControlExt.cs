using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._ES.UI.Controls;

public static class ControlExt
{
    [UsedImplicitly] // XAMLIL
    public static Alignment GetAlignment(Control c)
    {
        return new Alignment(c.HorizontalAlignment, c.VerticalAlignment);
    }

    [UsedImplicitly] // XAMLIL
    public static void SetAlignment(Control c, Alignment align)
    {
        c.HorizontalAlignment = align.Horizontal;
        c.VerticalAlignment = align.Vertical;
    }
}

public sealed class Alignment(Control.HAlignment horizontal, Control.VAlignment vertical)
{
    public Control.HAlignment Horizontal { get; set; } = horizontal;
    public Control.VAlignment Vertical { get; set; } = vertical;

    public static Alignment Parse(string s, IFormatProvider? provider)
    {
        var span = s.AsSpan().Trim();
        var splits = span.Split(' ');

        if (!splits.MoveNext())
            throw new Exception($"Expected at least two elements to parse for {typeof(Alignment)}.");

        var horizontal = Enum.Parse<Control.HAlignment>(span[splits.Current]);

        if (!splits.MoveNext())
            throw new Exception($"Expected at least two elements to parse for {typeof(Alignment)}.");

        var vertical = Enum.Parse<Control.VAlignment>(span[splits.Current]);

        if (splits.MoveNext())
            throw new Exception($"Expected at most two elements to parse for {typeof(Alignment)}.");

        return new(horizontal, vertical);
    }
}

