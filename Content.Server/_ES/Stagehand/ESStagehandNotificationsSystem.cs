using Content.Server.Chat.Managers;
using Content.Server.KillTracking;
using Content.Shared._ES.Auditions.Components;
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KillReportedEvent>(OnKillReported);
        SubscribeLocalEvent<ESObjectiveProgressChangedEvent>(OnObjectiveProgressChanged);
    }

    private void OnKillReported(ref KillReportedEvent ev)
    {
        if (!TryComp<ActorComponent>(ev.Entity, out var actor))
            return;

        string? msg = null;
        var severity = ESStagehandNotificationSeverity.Medium;

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
            severity = ESStagehandNotificationSeverity.High;
            if (!_player.TryGetSessionById(player.PlayerId, out var attackerSession) ||
                attackerSession.AttachedEntity is not { } attackerEnt)
            {
                msg = Loc.GetString("es-stagehand-notification-kill-player-unknown",
                    ("entity", ev.Entity),
                    ("username", actor.PlayerSession.Name));
            }
            else
            {
                msg = Loc.GetString("es-stagehand-notification-kill-player",
                    ("entity", ev.Entity),
                    ("username", actor.PlayerSession.Name),
                    ("attacker", attackerEnt),
                    ("attackerUsername", attackerSession.Name));
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
            // failed (1 -> <1, technically reversible so multiple msgs could spawn but cant think of any situations where that would actually occur?)
            case { NewProgress: < 1f, OldProgress: >= 1f }:
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
