using Content.Client.Resources;
using JetBrains.Annotations;
using Robust.Client.ResourceManagement;

namespace Content.Client._ES.UI.Controls;

[PublicAPI]
public sealed class IconExtension
{
    private IResourceCache _resourceCache;
    public string Name { get; }

    public IconExtension(string name)
    {
        _resourceCache = IoCManager.Resolve<IResourceCache>();
        Name = name;
    }

    public object ProvideValue()
    {
        return _resourceCache.GetTexture($"/Textures/_ES/Interface/{Name}.svg.192dpi.png");
    }
}
