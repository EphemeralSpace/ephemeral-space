using Content.Server.Chat.Managers;
using Content.Server.Pinpointer;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.SecretIdentity.Stalker;
using Content.Shared._ES.SecretIdentity.Stalker.Components;
using Content.Shared.Chat;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Robust.Server.Player;
using Robust.Shared.Utility;

namespace Content.Server._ES.SecretIdentity.Stalker;

public sealed partial class ESStalkerNotificationSystem : EntitySystem
{
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private ESStalkerTargetingSystem _stalkerTargeting = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESStalkerTargetComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<ESStalkerTargetComponent, ESPlayerKilledEvent>(OnPlayerKilled);
    }

    private void OnMobStateChanged(Entity<ESStalkerTargetComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.OldMobState != MobState.Alive && args.NewMobState != MobState.Critical)
            return;

        if (!TryComp<MindComponent>(ent.Comp.OwningMind, out var mindComponent))
            return;

        var locationString = FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString(ent.Owner));

        var msg = Loc.GetString("es-stalker-notif-crit", ("location", locationString));
        var wrappedMsg = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        if (_player.TryGetSessionById(mindComponent.UserId, out var session))
            _chat.ChatMessageToOne(ChatChannel.Server, msg, wrappedMsg, default, false, session.Channel, Color.Red);
    }

    private void OnPlayerKilled(Entity<ESStalkerTargetComponent> ent, ref ESPlayerKilledEvent args)
    {
        _stalkerTargeting.UpdateStalkerObjectives(ent.Comp.OwningMind);
    }
}
