using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._ES.UI.Controls;

[PublicAPI]
public sealed class EButtonGroupExtension
{
    public bool IsNoneSetAllowed { get; }

    public EButtonGroupExtension(bool isNoneSetAllowed)
    {
        IsNoneSetAllowed = isNoneSetAllowed;
    }

    public object ProvideValue()
    {
        return new ButtonGroup(IsNoneSetAllowed);
    }
}
