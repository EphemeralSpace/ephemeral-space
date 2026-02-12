using Content.Server._ES.Masks.Avenger.Components;
using Content.Server.Actions;
using Content.Server.Chat.Managers;
using Content.Server.Pinpointer;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.Objectives.Target;
using Content.Shared.Chat;
using Content.Shared.Mind;
using Robust.Server.Player;
using Robust.Shared.Utility;

namespace Content.Server._ES.Masks.Avenger;

public sealed class ESAvengeOnKillObjectiveSystem : ESBaseTargetObjectiveSystem<ESAvengeOnKillObjectiveComponent>
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;

    public override Type[] TargetRelayComponents { get; } = [typeof(ESAvengeOnKillObjectiveMarkerComponent)];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESAvengeOnKillObjectiveMarkerComponent, ESPlayerKilledEvent>(OnKillReported);
    }

    private void OnKillReported(Entity<ESAvengeOnKillObjectiveMarkerComponent> ent, ref ESPlayerKilledEvent args)
    {
        foreach (var avenge in GetTargetingObjectives(ent))
        {
            AvengeKill(avenge, args);
        }
    }

    private void AvengeKill(Entity<ESAvengeOnKillObjectiveComponent> avenge, ESPlayerKilledEvent args)
    {
        if (!ObjectivesSys.TryFindObjectiveHolder(avenge.Owner, out var holder))
            return;

        var killedHadObjective = false;
        if (MindSys.TryGetMind(args.Killed, out var killedMind, out _))
            killedHadObjective = ObjectivesSys.HasObjective(killedMind, avenge);

        var validKill = !(args.Suicide || killedHadObjective || args.Killer is null);

        if (TryComp<MindComponent>(holder, out var mind) &&
            _player.TryGetSessionById(mind.UserId, out var session))
        {
            var name = Name(args.Killed);
            var locationString = FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString(args.Killed));

            var locale = validKill ? "es-avenger-die-message-kill" : "es-avenger-die-message";

            var msg = Loc.GetString(locale, ("name", name), ("location", locationString));
            var wrappedMsg = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
            _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMsg, default, false, session.Channel, Color.Red);
        }

        // Check for
        // - if the victim killed themselves
        // - if the victim died via the environment
        // - if the killer was actually the person who had to protect this person.
        if (validKill)
            return;

        if (!ObjectivesSys.TryAddObjective(holder.Value.AsNullable(), avenge.Comp.AvengeObjective, out var objective))
            return;
        TargetObjective.SetTarget(objective.Value.Owner, args.Killer!.Value);

        if (mind?.OwnedEntity is { } body)
            _actions.AddAction(body, avenge.Comp.ActionPrototype);
    }
}
