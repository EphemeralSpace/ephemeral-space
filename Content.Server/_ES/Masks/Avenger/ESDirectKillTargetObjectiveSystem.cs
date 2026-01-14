using Content.Server._ES.Masks.Avenger.Components;
using Content.Server.Chat.Managers;
using Content.Server.KillTracking;
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

        SubscribeLocalEvent<ESDirectKillTargetObjectiveMarkerComponent, KillReportedEvent>(OnKillReported);
    }

    private void OnKillReported(Entity<ESDirectKillTargetObjectiveMarkerComponent> ent, ref KillReportedEvent args)
    {
        if (args.Primary is not KillPlayerSource source ||
            !MindSys.TryGetMind(source.PlayerId, out var mind))
            return;

        foreach (var objective in ObjectivesSys.GetObjectives<ESDirectKillTargetObjectiveComponent>(mind.Value.Owner))
        {
            if (TargetObjective.GetTargetOrNull(objective.Owner) != args.Entity)
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
