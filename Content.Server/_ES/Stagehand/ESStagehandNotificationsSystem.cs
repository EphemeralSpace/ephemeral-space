using Content.Server.Chat.Managers;
using Content.Server.Mind;
using Content.Shared._ES.Auditions.Components;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Components;
using Content.Shared._ES.Stagehand.Components;
using Content.Shared.Chat;
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
    [Dependency] private readonly ESSharedObjectiveSystem _objectives = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESPlayerKilledEvent>(OnKillReported);
        SubscribeLocalEvent<ESObjectiveProgressChangedEvent>(OnObjectiveProgressChanged);
    }

    private void OnKillReported(ref ESPlayerKilledEvent ev)
    {
        if (!_mind.TryGetMind(ev.Killed, out var killedMind) ||
            !killedMind.Value.Comp.OriginalOwnerUserId.HasValue ||
            !_player.TryGetPlayerData(killedMind.Value.Comp.OriginalOwnerUserId.Value, out var data))
            return;

        var killedUserName = data.UserName;

        string? msg = null;
        var severity = ESStagehandNotificationSeverity.Medium;

        if (ev.Suicide)
        {
            msg = Loc.GetString("es-stagehand-notification-kill-suicide",
                ("entity", ev.Killed),
                ("username", killedUserName));
        }
        else if (ev.Environment)
        {
            msg = Loc.GetString("es-stagehand-notification-kill-environment",
                ("entity", ev.Killed),
                ("username", killedUserName));
        }
        else if (ev.Killer is { } killer)
        {
            severity = ESStagehandNotificationSeverity.High;

            if (_mind.TryGetMind(killer, out var mind ) &&
                mind.Value.Comp.OriginalOwnerUserId is { } userId &&
                _player.TryGetPlayerData(userId, out var killerData))
            {
                msg = Loc.GetString("es-stagehand-notification-kill-player",
                    ("entity", ev.Killed),
                    ("username", killedUserName),
                    ("attacker", killer),
                    ("attackerUsername", killerData.UserName));
            }
            else
            {
                msg = Loc.GetString("es-stagehand-notification-kill-player-userless",
                    ("entity", ev.Killed),
                    ("username", killedUserName),
                    ("attacker", killer));
            }
        }

        if (msg != null)
            SendStagehandNotification(msg, severity);
    }

    private void OnObjectiveProgressChanged(ref ESObjectiveProgressChangedEvent ev)
    {
        LocId? msgId;

        switch (ev)
        {
            // Only announce relevant situations
            // just completed
            case { NewProgress: >= 1f, OldProgress: < 1f }:
                msgId = "es-stagehand-notification-objective-completed";
                break;
            // failed
            case { NewProgress: <= 0f, OldProgress: > 0f }:
                msgId = "es-stagehand-notification-objective-failed";
                break;
            default:
                return;
        }

        if (msgId == null)
            return;

        // since we know it's significant, figure out the holding entity
        if (!_objectives.TryFindObjectiveHolder((ev.Objective.Owner, ev.Objective.Comp), out var holder))
            return;

        var entityName = Name(holder.Value);
        if (TryComp<ESCharacterComponent>(holder.Value, out var comp))
            entityName = comp.Name;

        var resolvedMessage = Loc.GetString(msgId, ("entity", entityName), ("objective", ev.Objective.Owner));
        SendStagehandNotification(resolvedMessage);
    }

    /// <summary>
    ///     Sends a notification message to all currently active stagehands, formatted correctly.
    /// </summary>
    /// <param name="msg">An already-resolved string to use as the message.</param>
    /// <param name="severity">The severity of this notification, defaulting to medium (regular size)</param>
    [PublicAPI]
    public void SendStagehandNotification(string msg, ESStagehandNotificationSeverity severity = ESStagehandNotificationSeverity.Medium)
    {
        var stagehands = new List<INetChannel>();
        var query = EntityQueryEnumerator<ESStagehandComponent, ActorComponent>();
        while (query.MoveNext(out _, out _, out var actor))
        {
            stagehands.Add(actor.PlayerSession.Channel);
        }

        var locId = severity switch
        {
            ESStagehandNotificationSeverity.Low => "es-stagehand-notification-wrap-message-low",
            ESStagehandNotificationSeverity.Medium => "es-stagehand-notification-wrap-message-medium",
            _ => "es-stagehand-notification-wrap-message-high",
        };

        var wrappedMsg = Loc.GetString(locId, ("message", msg));
        _chat.ChatMessageToMany(ChatChannel.Server, msg, wrappedMsg, default, false, true, stagehands, Color.Plum);
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
