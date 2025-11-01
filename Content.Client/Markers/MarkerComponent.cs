using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Markers
{
    [RegisterComponent]
    public sealed partial class MarkerComponent : Component
    {
        // ES START
        // used to only toggle specific layers rather than the entire sprite
        [DataField]
        public HashSet<string>? Layers;
        // ES END
    }
}
