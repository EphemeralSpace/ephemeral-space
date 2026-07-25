using Content.Server.Chat.Managers;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Components;
using Content.Shared._ES.Stagehand;
using Content.Shared._ES.Stagehand.Components;
using Content.Shared.Chat;
using Content.Shared.Mind;
using JetBrains.Annotations;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._ES.Stagehand;

/// <summary>
///     Handles sending stagehand notifications for various non-stagehand events ingame: objective completions, deaths, etc.
/// </summary>
public sealed partial class ESStagehandNotificationsSystem : ESSharedStagehandNotificationsSystem
{
    [Dependency] private ESSharedObjectiveSystem _objectives = default!;
    [Dependency] private IChatManager _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESPlayerKilledEvent>(OnKillReported);
        SubscribeLocalEvent<ESObjectiveProgressChangedEvent>(OnObjectiveProgressChanged);
    }

    private void OnKillReported(ref ESPlayerKilledEvent ev)
    {
        if (!Mind.TryGetMind(ev.Killed, out _))
            return;

        string? msg = null;
        var severity = ESStagehandNotificationSeverity.Medium;

        if (ev.Suicide)
        {
            msg = Loc.GetString("es-stagehand-notification-kill-suicide",
                ("player", WrapEntityNameWithUsername(ev.Killed)));
        }
        else if (ev.Environment)
        {
            msg = Loc.GetString("es-stagehand-notification-kill-environment",
                ("player", WrapEntityNameWithUsername(ev.Killed)));
        }
        else if (ev.Killer is { } killer)
        {
            severity = ESStagehandNotificationSeverity.High;
            msg = Loc.GetString("es-stagehand-notification-kill-player",
                ("player", WrapEntityNameWithUsername(ev.Killed)),
                ("attacker", WrapEntityName(killer)));
        }

        if (msg != null)
            SendStagehandNotification(msg, severity);
    }

    private void OnObjectiveProgressChanged(ref ESObjectiveProgressChangedEvent ev)
    {
        if (!_objectives.IsObjectiveInitialized(ev.Objective.AsNullable())
            || !_objectives.ShouldAnnounceProgress(ev.Objective.AsNullable()))
            return;

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
        if (TryComp<MindComponent>(holder.Value, out var mind) && mind.OwnedEntity is { } owned)
            entityName = WrapEntityName(owned);

        var resolvedMessage = Loc.GetString(msgId, ("entity", entityName), ("objective", ev.Objective.Owner));
        SendStagehandNotification(resolvedMessage);
    }

    /// <summary>
    ///     Sends a notification message to all currently active stagehands, formatted correctly.
    /// </summary>
    /// <param name="msg">An already-resolved string to use as the message.</param>
    /// <param name="severity">The severity of this notification, defaulting to medium (regular size)</param>
    [PublicAPI]
    public override void SendStagehandNotification(string msg, ESStagehandNotificationSeverity severity = ESStagehandNotificationSeverity.Medium)
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
