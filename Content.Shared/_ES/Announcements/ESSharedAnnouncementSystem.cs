using Content.Shared.Chat;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Shared._ES.Announcements;

/// <summary>
///     API for doing announcements, pulled out of <see cref="SharedChatSystem"/>.
///     Handles logic for queueing announcements and not playing them at the same time, as well as other things like
///     announcements cutting out to play a different announcement.
/// </summary>
public abstract class ESSharedAnnouncementSystem : EntitySystem
{

    /// <summary>
    ///     Dispatches an announcement to all players in the server.
    /// </summary>
    /// <param name="message">The contents of the message.</param>
    /// <param name="sender">The sender (Communications Console in Communications Console Announcement).</param>
    /// <param name="playSound">Play the announcement sound.</param>
    /// <param name="announcementSound">Sound to play.</param>
    /// <param name="colorOverride">Optional color for the announcement message.</param>
    /// <param name="important">Will stop the currently playing announcement sound if one exists to play this instead.</param>
    public virtual void DispatchGlobalAnnouncement(
        string message,
        string? sender = null,
        bool playSound = true,
        SoundSpecifier? announcementSound = null,
        Color? colorOverride = null,
        bool important = false)
    {
    }

    /// <summary>
    ///     Dispatches an announcement to everyone in the current round.
    /// </summary>
    /// <param name="source">The entity making the announcement (used to determine the station).</param>
    /// <param name="message">The contents of the message.</param>
    /// <param name="sender">The sender (Communications Console in Communications Console Announcement).</param>
    /// <param name="playSound">Play the announcement sound.</param>
    /// <param name="announcementSound">Sound to play.</param>
    /// <param name="colorOverride">Optional color for the announcement message.</param>
    /// <param name="important">Will stop the currently playing announcement sound if one exists to play this instead.</param>
    public virtual void DispatchRoundAnnouncement(
        EntityUid source,
        string message,
        string? sender = null,
        bool playSound = true,
        SoundSpecifier? announcementSound = null,
        Color? colorOverride = null,
        bool important = false)
    {
    }
}
