using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Shared._ES.Lobby.Components;
using Content.Shared._ES.Stagehand;
using Content.Shared.Database;
using Content.Shared.Mind;
using Content.Shared.Players;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Stagehand;

/// <summary>
/// This handles logic for spawning in stagehands into the round.
/// </summary>
public sealed class ESStagehandSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    private static readonly EntProtoId StagehandPrototype = "ESMobStagehand";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeNetworkEvent<ESJoinStagehandMessage>(OnJoinStagehand);
    }

    private void OnJoinStagehand(ESJoinStagehandMessage args, EntitySessionEventArgs msg)
    {
        if (msg.SenderSession.AttachedEntity is not { } entity)
            return;

        if (_gameTicker.RunLevel != GameRunLevel.InRound)
            return;

        if (!HasComp<ESTheatergoerMarkerComponent>(entity))
            return;

        // TODO: prevent rejoining multiple times

        _gameTicker.PlayerJoinGame(msg.SenderSession);
        SpawnStagehand(msg.SenderSession);
    }

    public void SpawnStagehand(ICommonSession player)
    {
        if (_gameTicker.GetObserverSpawnPoint() is not { EntityId.Id: > 0 } coords)
            return;

        Entity<MindComponent?>? mind = player.GetMind();
        if (mind == null)
        {
            var name = _gameTicker.GetPlayerProfile(player).Name;
            var (mindId, mindComp) = _mind.CreateMind(player.UserId, name);
            mind = (mindId, mindComp);
            mindComp.PreventGhosting = true;
            _mind.SetUserId(mind.Value, player.UserId);
        }

        var stagehand = SpawnAtPosition(StagehandPrototype, coords);
        _mind.TransferTo(mind.Value, stagehand, mind: mind.Value);

        _adminLog.Add(LogType.Mind, $"{ToPrettyString(mind.Value):player} became a stagehand.");
    }
}
