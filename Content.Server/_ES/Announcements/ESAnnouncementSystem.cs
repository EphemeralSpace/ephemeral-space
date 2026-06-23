using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared._ES.Announcements;
using Content.Shared._ES.Core.Timer;
using Content.Shared.Chat;
using Content.Shared.Database;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._ES.Announcements;

/// <inheritdoc />
public sealed partial class ESAnnouncementSystem : ESSharedAnnouncementSystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private ESEntityTimerSystem _timer = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly TimeSpan MinTimeBetweenAnnouncements = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan ImmediateAnnouncementCutoffDelay = TimeSpan.FromSeconds(1);
    private static readonly SoundSpecifier DefaultAnnouncementSound = new SoundPathSpecifier("/Audio/_ES/Announcements/attention_low.ogg");
    private static readonly SoundSpecifier AnnouncementCutoffSound = new SoundPathSpecifier("/Audio/_ES/Announcements/cutoff.ogg");

    // priority announcements, these will be checked first and do not respect min time
    // if one is played, the last announcement will cut off and play this instead
    private readonly Queue<QueuedAnnouncement> _immediateAnnouncements = new();
    // regular announcements which respect the min time
    private readonly Queue<QueuedAnnouncement> _queuedAnnouncements = new();

    private TimeSpan? _lastAnnouncementTime;
    private (EntityUid, AudioComponent)? _currentlyPlayingAnnouncementSound;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        while (_immediateAnnouncements.TryDequeue(out var immediateAnnouncement))
        {
            if (_audio.IsPlaying(_currentlyPlayingAnnouncementSound?.Item1, _currentlyPlayingAnnouncementSound?.Item2))
            {
                _audio.Stop(_currentlyPlayingAnnouncementSound?.Item1, _currentlyPlayingAnnouncementSound?.Item2);
                _audio.PlayGlobal(AnnouncementCutoffSound, immediateAnnouncement.Filter, true);

                _timer.SpawnMethodTimer(ImmediateAnnouncementCutoffDelay,
                    () => DoQueuedAnnouncement(immediateAnnouncement));
            }
            else
            {
                DoQueuedAnnouncement(immediateAnnouncement);
            }

            _lastAnnouncementTime = _timing.CurTime;
        }

        if (_lastAnnouncementTime != null && _timing.CurTime < (_lastAnnouncementTime + MinTimeBetweenAnnouncements))
            return;

        if (!_queuedAnnouncements.TryDequeue(out var announcement))
            return;

        DoQueuedAnnouncement(announcement);
        _lastAnnouncementTime = _timing.CurTime;
    }

    private void DoQueuedAnnouncement(QueuedAnnouncement announcement)
    {
        _chatManager.ChatMessageToManyFiltered(announcement.Filter,
            ChatChannel.Radio,
            announcement.Message,
            announcement.WrappedMessage,
            announcement.Source,
            false,
            true,
            announcement.Color);

        if (announcement.Sound != null)
            _currentlyPlayingAnnouncementSound = _audio.PlayGlobal(announcement.Sound, announcement.Filter, true, AudioParams.Default.WithVolume(-2f));
    }

    private void QueueAnnouncement(Filter filter,
        string message,
        string wrappedMessage,
        EntityUid source,
        SoundSpecifier? sound,
        Color? color,
        bool important)
    {
        var queue = important ? _immediateAnnouncements : _queuedAnnouncements;
        var announcement = new QueuedAnnouncement(filter, message, wrappedMessage, source, sound, color);
        queue.Enqueue(announcement);
    }

    /// <inheritdoc />
    public override void DispatchGlobalAnnouncement(
        string message,
        string? sender = null,
        bool playSound = true,
        SoundSpecifier? announcementSound = null,
        Color? colorOverride = null,
        bool important = false)
    {
        sender ??= Loc.GetString("chat-manager-sender-announcement");
        var wrappedMessage = Loc.GetString("chat-manager-sender-announcement-wrap-message", ("sender", sender), ("message", FormattedMessage.EscapeText(message)));
        var filter = Filter.Broadcast();
        var sound = playSound ? (announcementSound ?? DefaultAnnouncementSound) : null;
        QueueAnnouncement(filter, message, wrappedMessage, default, sound, colorOverride, important);

        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Global announcement from {sender}: {message}");
    }

    /// <inheritdoc />
    public override void DispatchRoundAnnouncement(
        EntityUid source,
        string message,
        string? sender = null,
        bool playSound = true,
        SoundSpecifier? announcementSound = null,
        Color? colorOverride = null,
        bool important = false)
    {
        sender ??= Loc.GetString("chat-manager-sender-announcement");
        var wrappedMessage = Loc.GetString("chat-manager-sender-announcement-wrap-message", ("sender", sender), ("message", FormattedMessage.EscapeText(message)));
        var filter = Filter.Empty().AddWhere(_ticker.UserHasJoinedGame);
        var sound = playSound ? (announcementSound ?? DefaultAnnouncementSound) : null;
        QueueAnnouncement(filter, message, wrappedMessage, source, sound, colorOverride, important);

        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Round Announcement on {source} from {sender}: {message}");
    }

    /// <summary>
    ///     Stores data for an announcement to be played later.
    /// </summary>
    private record struct QueuedAnnouncement(
        Filter Filter,
        string Message,
        string WrappedMessage,
        EntityUid Source,
        SoundSpecifier? Sound,
        Color? Color);
}
