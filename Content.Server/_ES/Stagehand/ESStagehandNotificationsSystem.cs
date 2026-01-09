using Content.Server.Chat.Managers;
using Content.Server.KillTracking;
using Content.Shared._ES.Stagehand.Components;
using Content.Shared.Chat;
using Content.Shared.Players;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._ES.Stagehand;

/// <summary>
///     Handles sending stagehand notifications for various non-stagehand events ingame: objective completions, deaths, etc.
/// </summary>
public sealed class ESStagehandNotificationsSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KillReportedEvent>(OnKillReported);
    }

    private void OnKillReported(KillReportedEvent ev)
    {
        if (!TryComp<ActorComponent>(ev.Entity, out var actor))
            return;

        string? msg = null;

        if (ev.Suicide)
        {
            msg = Loc.GetString("es-stagehand-notification-kill-suicide",
                ("entity", ev.Entity),
                ("username", actor.PlayerSession.Name));
        }
        else if (ev.Primary is KillEnvironmentSource)
        {
            msg = Loc.GetString("es-stagehand-notification-kill-environment",
                ("entity", ev.Entity),
                ("username", actor.PlayerSession.Name));
        }
        else if (ev.Primary is KillNpcSource npc)
        {
            msg = Loc.GetString("es-stagehand-notification-kill-npc",
                ("entity", ev.Entity),
                ("username", actor.PlayerSession.Name),
                ("attacker", npc.NpcEnt));
        }
        else if (ev.Primary is KillPlayerSource player)
        {
            if (!_player.TryGetSessionById(player.PlayerId, out var attackerSession) || attackerSession.AttachedEntity is not { } attackerEnt)
                return;

            msg = Loc.GetString("es-stagehand-notification-kill-player",
                ("entity", ev.Entity),
                ("username", actor.PlayerSession.Name),
                ("attacker", attackerEnt),
                ("attackerUsername", attackerSession.Name));
        }

        if (msg != null)
            SendStagehandNotification(msg);
    }

    /// <summary>
    ///     Sends a notification message to all currently active stagehands, formatted correctly.
    /// </summary>
    /// <param name="msg">An already-resolved string to use as the message.</param>
    /// <param name="severity">The severity of this notification, defaulting to medium (regular size)</param>
    [PublicAPI]
    public void SendStagehandNotification(string msg, ESStagehandNotificationSeverity severity = ESStagehandNotificationSeverity.Medium)
    {
        var voters = new List<INetChannel>();
        var query = EntityQueryEnumerator<ESStagehandComponent, ActorComponent>();
        while (query.MoveNext(out _, out _, out var actor))
        {
            voters.Add(actor.PlayerSession.Channel);
        }

        var locId = severity switch
        {
            ESStagehandNotificationSeverity.Low => "es-stagehand-notification-wrap-message-low",
            ESStagehandNotificationSeverity.Medium => "es-stagehand-notification-wrap-message-medium",
            _ => "es-stagehand-notification-wrap-message-high",
        };

        var wrappedMsg = Loc.GetString(locId, ("message", msg));
        _chat.ChatMessageToMany(ChatChannel.Server, msg, wrappedMsg, default, false, true, voters, Color.Plum);
    }
}

/// <summary>
///     Determines the font size and styling of the message sent to stagehands.
/// </summary>
public enum ESStagehandNotificationSeverity : byte
{
    Low,
    Medium,
    High
}
