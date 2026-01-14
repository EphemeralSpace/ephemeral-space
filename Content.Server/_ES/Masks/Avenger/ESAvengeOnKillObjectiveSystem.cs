using Content.Server._ES.Masks.Avenger.Components;
using Content.Server.Actions;
using Content.Server.Chat.Managers;
using Content.Server.KillTracking;
using Content.Server.Pinpointer;
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

        SubscribeLocalEvent<ESAvengeOnKillObjectiveMarkerComponent, KillReportedEvent>(OnKillReported);
    }

    private void OnKillReported(Entity<ESAvengeOnKillObjectiveMarkerComponent> ent, ref KillReportedEvent args)
    {
        foreach (var avenge in GetTargetingObjectives(ent))
        {
            AvengeKill(avenge, args);
        }
    }

    private void AvengeKill(Entity<ESAvengeOnKillObjectiveComponent> avenge, KillReportedEvent args)
    {
        if (!ObjectivesSys.TryFindObjectiveHolder(avenge.Owner, out var holder))
            return;

        // This isn't really great but eh. it'll do.
        if (TryComp<MindComponent>(holder, out var mind) &&
            _player.TryGetSessionById(mind.UserId, out var session))
        {
            var name = Name(args.Entity);
            var locationString = FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString(args.Entity));

            // Literally just fuck this API i do not give the shit
            var isSuicide = args.Suicide ||
                         args.Primary is not KillPlayerSource src ||
                         !MindSys.TryGetMind(src.PlayerId, out var m) ||
                         !m.Value.Comp.OwnedEntity.HasValue ||
                         ObjectivesSys.HasObjective(m.Value, avenge);
            var locale = isSuicide ? "es-avenger-die-message" : "es-avenger-die-message-kill";

            var msg = Loc.GetString(locale, ("name", name), ("location", locationString));
            var wrappedMsg = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
            _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMsg, default, false, session.Channel, Color.Red);
        }

        // Check for
        // - if the victim killed themselves
        // - if the victim wasn't killed by a player
        // - if the killer was actually the person who had to protect this person.
        if (args.Suicide ||
            args.Primary is not KillPlayerSource source ||
            !MindSys.TryGetMind(source.PlayerId, out var killerMind) ||
            killerMind.Value.Comp.OwnedEntity is not { } killerBody ||
            ObjectivesSys.HasObjective(killerMind.Value, avenge))
        {
            return;
        }

        if (!ObjectivesSys.TryAddObjective(holder.Value.AsNullable(), avenge.Comp.AvengeObjective, out var objective))
            return;
        TargetObjective.SetTarget(objective.Value.Owner, killerBody);

        if (mind?.OwnedEntity is { } body)
            _actions.AddAction(body, avenge.Comp.ActionPrototype);
    }
}
