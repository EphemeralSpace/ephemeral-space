using Content.Server._ES.Masks.Avenger.Components;
using Content.Server.Chat.Managers;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.Objectives.Target;
using Content.Shared.Chat;
using Robust.Server.Player;

namespace Content.Server._ES.Masks.Avenger;

public sealed class ESDirectKillTargetObjectiveSystem : ESBaseTargetObjectiveSystem<ESDirectKillTargetObjectiveComponent>
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override Type[] TargetRelayComponents { get; } = [typeof(ESDirectKillTargetObjectiveMarkerComponent)];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESDirectKillTargetObjectiveMarkerComponent, ESPlayerKilledEvent>(OnKillReported);
    }

    private void OnKillReported(Entity<ESDirectKillTargetObjectiveMarkerComponent> ent, ref ESPlayerKilledEvent args)
    {
        // we only care about kills with an actual associated killer
        if (!args.ValidKill)
            return;

        // We need the killer's mind since we need to ensure that the targeting objective
        // that we increment is the one that belongs to the killer.
        if (!MindSys.TryGetMind(args.Killer.Value, out var killerMind))
            return;

        foreach (var objective in GetTargetingObjectives(ent))
        {
            // If the objective doesn't belong to the person who got the kill
            // then we want to ignore it and not increment the counter.
            if (!ObjectivesSys.HasObjective(killerMind.Value, objective))
                continue;

            // Increment and count the kill
            ObjectivesSys.AdjustObjectiveCounter(objective.Owner);

            if (objective.Comp.SuccessMessage.HasValue && _player.TryGetSessionById(killerMind.Value.Comp.UserId, out var session))
            {
                var msg = Loc.GetString(objective.Comp.SuccessMessage, ("name", Name(args.Killed)));
                var wrappedMsg = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
                _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMsg, default, false, session.Channel, Color.Pink);
            }
        }
    }
}
