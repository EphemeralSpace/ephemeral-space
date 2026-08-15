using Content.Server._ES.SecretIdentity.Objectives.Relays.Components;
using Content.Server._ES.SecretIdentity.Phantom.Components;
using Content.Shared._ES.Chat;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Target;
using Robust.Server.Player;

namespace Content.Server._ES.SecretIdentity.Phantom;

public sealed partial class ESAvengeSelfObjectiveSystem : ESBaseObjectiveSystem<ESAvengeSelfObjectiveComponent>
{
    [Dependency] private IESSharedChatManager _chat = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private ESTargetObjectiveSystem _targetObjective = default!;

    public override Type[] RelayComponents => [typeof(ESKilledRelayComponent)];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESAvengeSelfObjectiveComponent, ESPlayerKilledEvent>(OnKillReported);
    }

    private void OnKillReported(Entity<ESAvengeSelfObjectiveComponent> ent, ref ESPlayerKilledEvent args)
    {
        if (!ObjectivesSys.TryFindObjectiveHolder(ent.Owner, out var holder))
            return;

        if (!args.ValidKill ||
            !MindSys.TryGetMind(args.Killer.Value, out _, out var mindComp) ||
            mindComp.OwnedEntity is not { } body)
        {
            return;
        }

        if (!ObjectivesSys.TryAddObjective(holder.Value.AsNullable(), ent.Comp.AvengeObjective, out var objective))
            return;

        _targetObjective.SetTarget(objective.Value.Owner, body);

        if (_player.TryGetSessionByEntity(args.Killed, out var session))
        {
            var msg = Loc.GetString(ent.Comp.SuccessMessage);
            _chat.SendServerMessage(msg, session, Color.Red);
        }
    }
}
