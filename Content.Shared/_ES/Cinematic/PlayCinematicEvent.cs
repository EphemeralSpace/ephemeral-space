using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Cinematic;

/// <summary>
///     Network event sent from server to client to make them start playing a given cinematic.
///     This can also be raised locally on the client.
/// </summary>
[Serializable, NetSerializable]
public sealed class PlayCinematicEvent : EntityEventArgs
{
    public ProtoId<ESCinematicPrototype> Cinematic;
}
