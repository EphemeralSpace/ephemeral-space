using Robust.Shared.Utility;

namespace Content.Client._ES.Breakable.Components;

[RegisterComponent]
public sealed partial class ESBreakableVisualsComponent : Component
{
    [DataField]
    public Dictionary<string, SpriteSpecifier> BaseLayers = new();

    [DataField]
    public Dictionary<string, SpriteSpecifier> BrokenLayers = new();
}
