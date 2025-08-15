using Content.Server.Chat.Systems;
using Content.Server.RoundEnd;
using Content.Shared._ES.Evac;
using Content.Shared._ES.Evac.Components;

namespace Content.Server._ES.Evac;

/// <inheritdoc/>
public sealed class ESEvacSystem : ESSharedEvacSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;

    public override void SetEvacVote(Entity<ESEvacStationComponent> ent, bool value, string? overrideMessage = null)
    {
        if (ent.Comp.EvacVoteEnabled == value)
            return;

        var msg = overrideMessage ?? (value
            ? Loc.GetString("es-evac-announcement-signal-enabled")
            : Loc.GetString("es-evac-announcement-signal-disabled"));
        _chat.DispatchStationAnnouncement(ent, msg);
        base.SetEvacVote(ent, value, overrideMessage);
    }

    protected override void UpdateEvacVoteStatus()
    {
        base.UpdateEvacVoteStatus();

        if (_roundEnd.IsRoundEndRequested())
            return;

        var yesCount = 0;
        var noCount = 0;
        var query = EntityQueryEnumerator<ESEvacStationComponent>();
        while (query.MoveNext(out var comp))
        {
            if (comp.EvacVoteEnabled)
                yesCount++;
            else
                noCount++;
        }

        if ((float)yesCount / (noCount + yesCount) < EvacVotePercentage)
            return;
        _roundEnd.RequestRoundEnd(checkCooldown: false);

        var query2 = EntityQueryEnumerator<ESEvacStationComponent>();
        while (query2.MoveNext(out var uid, out var comp))
        {
            comp.Locked = true;
            comp.RoundEndTime = _roundEnd.ExpectedCountdownEnd;
            Dirty(uid, comp);
        }
    }
}
