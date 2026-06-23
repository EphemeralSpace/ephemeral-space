using Content.Server._ES.Announcements;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Components;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

/// <summary>
///     An abstract entity system inherited by all station events for their behavior.
/// </summary>
public abstract partial class StationEventSystem<T> : GameRuleSystem<T> where T : IComponent
{
    [Dependency] protected IAdminLogManager AdminLogManager = default!;
    [Dependency] protected IPrototypeManager PrototypeManager = default!;
    [Dependency] protected ChatSystem ChatSystem = default!;
    [Dependency] protected ESAnnouncementSystem AnnouncementSystem = default!;
    [Dependency] protected SharedAudioSystem Audio = default!;
    [Dependency] protected StationSystem StationSystem = default!;

    protected ISawmill Sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        Sawmill = Logger.GetSawmill("stationevents");
    }

    /// <inheritdoc/>
    protected override void Added(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;

        AdminLogManager.Add(LogType.EventAnnounced, $"Event added / announced: {ToPrettyString(uid)}");

        //ES Start
        if (stationEvent.StartAnnouncement != null)
            AnnouncementSystem.DispatchRoundAnnouncement(uid, Loc.GetString(stationEvent.StartAnnouncement), announcementSound: stationEvent.StartAudio, colorOverride: stationEvent.StartAnnouncementColor, sender: Loc.GetString("es-station-event-announcer"));
        //ES End
    }

    /// <inheritdoc/>
    protected override void Started(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;

        AdminLogManager.Add(LogType.EventStarted, LogImpact.High, $"Event started: {ToPrettyString(uid)}");

        if (stationEvent.Duration != null)
        {
            var duration = stationEvent.MaxDuration == null
                ? stationEvent.Duration
                : TimeSpan.FromSeconds(RobustRandom.NextDouble(stationEvent.Duration.Value.TotalSeconds,
                    stationEvent.MaxDuration.Value.TotalSeconds));
            stationEvent.EndTime = Timing.CurTime + duration;
        }
    }

    /// <inheritdoc/>
    protected override void Ended(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;

        AdminLogManager.Add(LogType.EventStopped, $"Event ended: {ToPrettyString(uid)}");

        //ES Start
        if (stationEvent.EndAnnouncement != null)
            AnnouncementSystem.DispatchRoundAnnouncement( uid, Loc.GetString(stationEvent.EndAnnouncement), announcementSound: stationEvent.EndAudio, colorOverride: stationEvent.EndAnnouncementColor, sender: Loc.GetString("es-station-event-announcer"));
        //ES End
    }

    /// <summary>
    ///     Called every tick when this event is running.
    ///     Events are responsible for their own lifetime, so this handles starting and ending after time.
    /// </summary>
    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StationEventComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var stationEvent, out var ruleData))
        {
            if (!GameTicker.IsGameRuleAdded(uid, ruleData))
                continue;

            if (!GameTicker.IsGameRuleActive(uid, ruleData) && !HasComp<DelayedStartRuleComponent>(uid))
            {
                GameTicker.StartGameRule(uid, ruleData);
            }
            else if (stationEvent.EndTime != null && Timing.CurTime >= stationEvent.EndTime && GameTicker.IsGameRuleActive(uid, ruleData))
            {
                GameTicker.EndGameRule(uid, ruleData);
            }
        }
    }
// ES START
    public void SetStartAnnouncement(Entity<StationEventComponent?> ent, string? announcement)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;
        ent.Comp.StartAnnouncement = announcement;
    }
// ES END
}
