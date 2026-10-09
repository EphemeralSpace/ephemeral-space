namespace Content.Shared._ES.Forensics;

public sealed class ESForensicsSystem : EntitySystem
{
    public void TransferForensics(EntityUid from, EntityUid to)
    {
        var ev = new ESTransferForensicsEvent(to);
        RaiseLocalEvent(from, ref ev);
    }
}

[ByRefEvent]
public readonly record struct ESTransferForensicsEvent(EntityUid Recipient);
