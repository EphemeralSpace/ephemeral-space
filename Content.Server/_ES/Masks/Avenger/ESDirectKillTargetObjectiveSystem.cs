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
        if (!args.ValidKill || !MindSys.TryGetMind(args.Killer.Value, out var mind))
            return;

        foreach (var objective in ObjectivesSys.GetObjectives<ESDirectKillTargetObjectiveComponent>(mind.Value.Owner))
        {
            if (TargetObjective.GetTargetOrNull(objective.Owner) != args.Killed)
                continue;

            ObjectivesSys.AdjustObjectiveCounter(objective.Owner);

            if (_player.TryGetSessionById(mind.Value.Comp.UserId, out var session))
            {
                var msg = Loc.GetString(objective.Comp.SuccessMessage);
                var wrappedMsg = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
                _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMsg, default, false, session.Channel, Color.Pink);
            }
        }
    }
}
