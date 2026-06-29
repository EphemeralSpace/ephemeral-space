using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared._ES.Lobby.Components;
using Content.Shared._ES.Stagehand;
using Content.Shared._ES.Stagehand.Components;
using Content.Shared.Database;
using Content.Shared.Follower;
using Content.Shared.Mind;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Stagehand;

/// <summary>
/// This handles logic for spawning in stagehands into the round.
/// </summary>
public sealed partial class ESStagehandSystem : EntitySystem
{
    [Dependency] private ESStagehandNotificationsSystem _notif = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private FollowerSystem _follower = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private RoleSystem _role = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly EntProtoId StagehandPrototype = "ESMobStagehand";
    private static readonly EntProtoId ObserverRole = "MindRoleObserver";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeNetworkEvent<ESJoinStagehandMessage>(OnJoinStagehand);
        SubscribeNetworkEvent<ESStagehandWarpMessage>(OnStagehandWarp);
    }

    private void OnJoinStagehand(ESJoinStagehandMessage args, EntitySessionEventArgs msg)
    {
        if (msg.SenderSession.AttachedEntity is not { } entity)
            return;

        if (_gameTicker.RunLevel == GameRunLevel.PreRoundLobby)
            return;

        if (!HasComp<ESTheatergoerMarkerComponent>(entity))
            return;

        _gameTicker.PlayerJoinGame(msg.SenderSession, silent: _gameTicker.UserHasJoinedGame(msg.SenderSession));
        SpawnStagehand(msg.SenderSession);
    }

    private void OnStagehandWarp(ESStagehandWarpMessage args, EntitySessionEventArgs msg)
    {
        if (msg.SenderSession.AttachedEntity is not { } entity)
            return;

        if (!HasComp<ESStagehandComponent>(entity))
            return;

        if (!TryGetEntity(args.Target, out var target))
            return;

        // Since the mind is stored in nullspace, we need to find the body and follow it directly.
        if (TryComp<MindComponent>(target, out var mind))
        {
            if (!mind.CurrentEntity.HasValue || TerminatingOrDeleted(mind.CurrentEntity))
                return;

            _follower.StartFollowingEntity(entity, mind.CurrentEntity.Value);
        }
        else
        {
            _follower.StartFollowingEntity(entity, target.Value);
        }
    }

    public EntityUid? SpawnStagehand(ICommonSession player, EntityCoordinates? position = null)
    {
        return SpawnStagehand(player.UserId, position);
    }

    public EntityUid? SpawnStagehand(NetUserId player, EntityCoordinates? position = null)
    {
        if (!position.HasValue)
        {
            position = _gameTicker.GetObserverSpawnPoint();
            if (!IsValidSpawnPosition(position))
                return null;
        }

        var name = _player.GetPlayerData(player).UserName;

        // Always make a new mind
        var mind = _mind.CreateMind(player, name);
        mind.Comp.PreventGhosting = true;
        _mind.SetUserId(mind, player);

        _role.MindAddRole(mind, ObserverRole);

        var stagehand = SpawnAtPosition(StagehandPrototype, position.Value);
        _mind.TransferTo(mind, stagehand, mind: mind);

        _notif.SendStagehandNotification(Loc.GetString("es-stagehand-notification-new-stagehand", ("username", name)));
        _adminLog.Add(LogType.Mind, $"{ToPrettyString(mind):player} became a stagehand.");

        return stagehand;
    }

    private bool IsValidSpawnPosition(EntityCoordinates? spawnPosition)
    {
        if (spawnPosition?.IsValid(EntityManager) != true)
            return false;

        var mapUid = _transform.GetMap(spawnPosition.Value);
        var gridUid = spawnPosition?.EntityId;
        // Test if the map is being deleted
        if (mapUid == null || TerminatingOrDeleted(mapUid.Value))
            return false;
        // Test if the grid is being deleted
        if (gridUid != null && TerminatingOrDeleted(gridUid.Value))
            return false;

        return true;
    }
}
