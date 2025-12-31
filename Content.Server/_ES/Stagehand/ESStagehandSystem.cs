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
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Stagehand;

/// <summary>
/// This handles logic for spawning in stagehands into the round.
/// </summary>
public sealed class ESStagehandSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly FollowerSystem _follower = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly RoleSystem _role = default!;

    private static readonly EntProtoId StagehandPrototype = "ESMobStagehand";
    private static readonly EntProtoId ObserverRole = "MindRoleObserver";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeNetworkEvent<ESJoinStagehandMessage>(OnJoinStagehand);
        SubscribeNetworkEvent<ESWarpToMindMessage>(OnWarpToMind);
    }

    private void OnJoinStagehand(ESJoinStagehandMessage args, EntitySessionEventArgs msg)
    {
        if (msg.SenderSession.AttachedEntity is not { } entity)
            return;

        if (_gameTicker.RunLevel == GameRunLevel.PreRoundLobby)
            return;

        if (!HasComp<ESTheatergoerMarkerComponent>(entity))
            return;

        // TODO: prevent rejoining multiple times

        _gameTicker.PlayerJoinGame(msg.SenderSession);
        SpawnStagehand(msg.SenderSession);
    }

    private void OnWarpToMind(ESWarpToMindMessage args, EntitySessionEventArgs msg)
    {
        if (msg.SenderSession.AttachedEntity is not { } entity)
            return;

        if (!HasComp<ESStagehandComponent>(entity))
            return;

        if (!TryGetEntity(args.Mind, out var targetMind) ||
            !TryComp<MindComponent>(targetMind, out var mind))
            return;

        if (!TryGetEntity(mind.OriginalOwnedEntity, out var target) ||
            TerminatingOrDeleted(target))
            return;

        _follower.StartFollowingEntity(entity, target.Value);
    }

    public void SpawnStagehand(ICommonSession player)
    {
        if (_gameTicker.GetObserverSpawnPoint() is not { EntityId.Id: > 0 } coords)
            return;

        // Always make a new mind
        var mind = _mind.CreateMind(player.UserId, player.Name);
        mind.Comp.PreventGhosting = true;
        _mind.SetUserId(mind, player.UserId);

        _role.MindAddRole(mind, ObserverRole);

        var stagehand = SpawnAtPosition(StagehandPrototype, coords);
        _mind.TransferTo(mind, stagehand, mind: mind);

        _adminLog.Add(LogType.Mind, $"{ToPrettyString(mind):player} became a stagehand.");
    }
}
