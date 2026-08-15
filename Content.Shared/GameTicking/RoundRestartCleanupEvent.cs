using Robust.Shared.Serialization;


namespace Content.Shared.GameTicking
{
    [Serializable, NetSerializable]
    public sealed class RoundRestartCleanupEvent : EntityEventArgs
    {
    }

    /// <summary>
    /// Event raised after the entities have been flushed and the round has been rest
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ESAfterRoundRestartCleanupEvent : EntityEventArgs
    {
    }
}
