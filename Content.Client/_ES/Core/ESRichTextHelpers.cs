using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._ES.Core;

public static class ESRichTextHelpers
{
    /// <summary>
    /// Sets a message on a richtext label, allowing any kind of formatting.
    /// Mostly a convenience and clarity thing since the default syntax in engine
    /// is complete annoying horseshit to type.
    /// </summary>
    public static RichTextLabel UnsafeSetMarkup(this RichTextLabel label, string message, Color? defaultColor = null)
    {
        label.SetMessage(FormattedMessage.FromMarkupPermissive(message), tagsAllowed: null, defaultColor: defaultColor);
        return label;
    }
}
