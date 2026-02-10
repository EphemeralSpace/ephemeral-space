using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Server._ES.Masks.Phantom.Components;
using Content.Server.Chat.Managers;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Target;
using Content.Shared.Chat;
using Robust.Server.Player;

namespace Content.Server._ES.Masks.Phantom;

public sealed class ESAvengeSelfObjectiveSystem : ESBaseObjectiveSystem<ESAvengeSelfObjectiveComponent>
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly ESTargetObjectiveSystem _targetObjective = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override Type[] RelayComponents => [typeof(ESKilledRelayComponent)];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESAvengeSelfObjectiveComponent, ESPlayerKilledEvent>(OnKillReported);
    }

    private void OnKillReported(Entity<ESAvengeSelfObjectiveComponent> ent, ref ESPlayerKilledEvent args)
    {
        var user = _player.TryGetSessionByEntity(args.Killed, out var session) ? session.Channel : null;
        string msg;

        if (!args.ValidKill ||
            !MindSys.TryGetMind(args.Killer.Value, out _, out var mindComp) ||
            mindComp.OwnedEntity is not { } body)
        {
            _metaData.SetEntityName(ent, Loc.GetString(ent.Comp.FailName));

            msg = Loc.GetString(ent.Comp.FailMessage);
        }
        else
        {
            _targetObjective.SetTarget(ent.Owner, body);

            msg = Loc.GetString(ent.Comp.SuccessMessage);
        }

        if (user != null)
        {
            var wrappedMsg = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
            _chat.ChatMessageToOne(ChatChannel.Server, msg, wrappedMsg, default, false, user, Color.Red);
        }
    }
}
