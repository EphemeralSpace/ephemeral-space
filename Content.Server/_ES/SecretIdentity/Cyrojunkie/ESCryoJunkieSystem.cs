using Content.Server._ES.Cryohusk;
using Content.Server._ES.Mind;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Managers;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.Cryohusk.Components;
using Content.Shared._ES.SecretIdentity.Cyrojunkie.Components;
using Content.Shared.Administration.Systems;
using Content.Shared.Atmos;
using Content.Shared.Chat;
using Content.Shared.Mind.Components;
using Robust.Server.Player;

namespace Content.Server._ES.SecretIdentity.Cyrojunkie;

public sealed partial class ESCryoJunkieSystem : EntitySystem
{
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private ESCryohuskSystem _cryo = default!;
    [Dependency] private ESEntityTimerSystem _entityTimer = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private RejuvenateSystem _rejuvenate = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ESCryoJunkieMindComponent, AutoGhostAttemptEvent>(OnGhostAttempt);
        SubscribeLocalEvent<MindContainerComponent, ESCyroJunkieTimerEvent>(OnCyroJunkieTimer);
    }

    private void OnCyroJunkieTimer(Entity<MindContainerComponent> ent, ref ESCyroJunkieTimerEvent args)
    {
        var mixture = _atmosphere.GetTileMixture(ent.Owner);
        mixture?.AdjustMoles(Gas.Cryogas, 200);

        _rejuvenate.PerformRejuvenate(ent);
        _cryo.Cryohusk(ent.Owner);
    }

    private void OnGhostAttempt(Entity<ESCryoJunkieMindComponent> ent, ref AutoGhostAttemptEvent args)
    {
        if (args.Mind.Comp.CurrentEntity is not { } owned)
            return;

        if (!HasComp<ESCryohuskableComponent>(owned))
            return;

        var msg = Loc.GetString("es-cryojunkie-implant-notif");
        var wrappedMsg = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        if (_player.TryGetSessionByEntity(owned, out var session))
            _chat.ChatMessageToOne(ChatChannel.Server, msg, wrappedMsg, default, false, session.Channel, Color.LightBlue);

        _entityTimer.SpawnTimer(owned, ent.Comp.HuskDelay, new ESCyroJunkieTimerEvent());

        args.Cancelled = true;
        RemComp(ent, ent.Comp);
    }
}
