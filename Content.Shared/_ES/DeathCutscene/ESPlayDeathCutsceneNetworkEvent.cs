using Robust.Shared.Serialization;

namespace Content.Shared._ES.DeathCutscene;

/// <summary>
///     Sent to a client to make them start the post-death cutscene sequence.
/// </summary>
[Serializable, NetSerializable]
public sealed class ESPlayDeathCutsceneNetworkEvent : EntityEventArgs;
