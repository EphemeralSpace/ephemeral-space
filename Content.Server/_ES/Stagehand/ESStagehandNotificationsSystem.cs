using Content.Server.Chat.Managers;
using Content.Server.Mind;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Components;
using Content.Shared._ES.Stagehand.Components;
using Content.Shared.Chat;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.GameObjects.Components.Localization;
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
        if (!_mind.TryGetMind(ev.Killed, out _))
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
    /// Version of <see cref="WrapEntityNameWithUsername"/> that formats relevant IC info into a name without giving a username.
    /// </summary>
    /// <remarks>
    /// Use when displaying an entity name but without the context of the username.
    /// </remarks>
    public string WrapEntityName(Entity<MindContainerComponent?> entity)
    {
        // Default case: basic entities display their entity name
        if (!Resolve(entity, ref entity.Comp, false) ||
            !_mind.TryGetMind(entity, out var mind, entity))
        {
            return Name(entity);
        }

        var entityName = Name(entity);
        var characterName = mind.Value.Comp.CharacterName ?? string.Empty;

        // If our name matches our body, just display the simple name.
        if (entityName.Equals(characterName, StringComparison.InvariantCulture))
        {
            return entityName;
        }

        if (TryComp<GrammarComponent>(entity, out var grammar) && grammar.ProperNoun == true)
        {
            return Loc.GetString("es-stagehand-notification-wrap-entity-body-player-swap",
                ("character", characterName),
                ("body", entityName));
        }

        return Loc.GetString("es-stagehand-notification-wrap-entity-body-mob-swap",
            ("character", characterName),
            ("body", entityName));
    }

    /// <summary>
    ///     Returns a string formatted like "entity name (players username)", for use in passing to <see cref="SendStagehandNotification"/>.
    /// </summary>
    /// <remarks>
    ///     You should **not** use this for all instances where an entity is mentioned.
    ///     Only reveal player usernames when they are dead, or about to die.
    /// </remarks>
    public string WrapEntityNameWithUsername(Entity<ActorComponent?> entity)
    {
        string? username = null;
        if (Resolve(entity, ref entity.Comp, false))
        {
            username = entity.Comp.PlayerSession.Name;
        }
        // try to get session from their mind
        else if (_mind.TryGetMind(entity, out var mind)
            && mind.Value.Comp.UserId is { } id
            && _player.TryGetPlayerData(id, out var sess))
        {
            username = sess.UserName;
        }
        else
        {
            return WrapEntityName(entity.Owner);
        }

        return Loc.GetString("es-stagehand-notification-wrap-entity-username",
            ("entity", WrapEntityName(entity.Owner)),
            ("username", username));
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
