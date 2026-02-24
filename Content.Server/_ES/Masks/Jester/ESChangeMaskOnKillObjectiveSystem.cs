using Content.Server._ES.Masks.Jester.Components;
using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Server.Chat.Managers;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Components;
using Content.Shared.Chat;
using Robust.Server.Player;

namespace Content.Server._ES.Masks.Jester;

public sealed class ESChangeMaskOnKillObjectiveSystem : ESBaseObjectiveSystem<ESChangeMaskOnKillObjectiveComponent>
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override Type[] RelayComponents => [typeof(ESKilledRelayComponent)];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESChangeMaskOnKillObjectiveComponent, ESKilledPlayerEvent>(OnKilledPlayer);
    }

    private void OnKilledPlayer(Entity<ESChangeMaskOnKillObjectiveComponent> ent, ref ESKilledPlayerEvent args)
    {
        if (args.Suicide)
            return;

        // Only matters if you kill a real player with a mind
        if (!MindSys.TryGetMind(args.Killed, out _))
            return;

        if (!MindSys.TryGetMind(args.Killer, out var mind))
            return;

        if (_player.TryGetSessionByEntity(args.Killer, out var session))
        {
            var msg = Loc.GetString(ent.Comp.Message);
            var wrappedMsg = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
            _chat.ChatMessageToOne(ChatChannel.Server, msg, wrappedMsg, default, false, session.Channel, Color.Red);
        }

        MaskSys.ChangeMask(mind.Value, ent.Comp.Mask);
    }

    protected override void GetObjectiveProgress(Entity<ESChangeMaskOnKillObjectiveComponent> ent, ref ESGetObjectiveProgressEvent args)
    {
        args.Progress = ent.Comp.DefaultProgress;
    }
}
