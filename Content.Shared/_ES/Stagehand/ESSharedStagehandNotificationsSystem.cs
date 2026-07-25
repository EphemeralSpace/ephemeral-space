using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Player;

namespace Content.Shared._ES.Stagehand;

public abstract partial class ESSharedStagehandNotificationsSystem : EntitySystem
{
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] protected SharedMindSystem Mind = default!;

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
            !Mind.TryGetMind(entity, out var mind, entity))
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
        string? username;
        if (Resolve(entity, ref entity.Comp, false))
        {
            username = entity.Comp.PlayerSession.Name;
        }
        // try to get session from their mind
        else if (Mind.TryGetMind(entity, out var mind)
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

    public virtual void SendStagehandNotification(
        string msg,
        ESStagehandNotificationSeverity severity = ESStagehandNotificationSeverity.Medium)
    {

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
