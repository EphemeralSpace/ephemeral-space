using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Crosshair;

/// <summary>
///     Raised on an entity with <see cref="ESCrosshairAimerComponent"/> as a predictive event on the client
/// </summary>
[Serializable, NetSerializable]
public sealed class ESCrosshairNetworkEvent : EntityEventArgs
{
    public MapCoordinates Coordinates;
}
