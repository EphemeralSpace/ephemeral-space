using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._ES.Auditions;
using Content.Server._ES.Masks.Components;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Shared._ES.Masks;
using Content.Shared.Chat;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Random.Helpers;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.Masks;

public sealed class ESMaskSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ESAuditionsSystem _esAuditions = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly JobSystem _job = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly RoleSystem _role = default!;

    private static readonly EntProtoId<ESMaskRoleComponent> MindRole = "ESMindRoleMask";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnRulePlayerJobsAssigned);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (!ev.LateJoin)
            return;
        AssignPlayersToTroupe([ev.Player]);
    }

    private void OnRulePlayerJobsAssigned(RulePlayerJobsAssignedEvent args)
    {
        AssignPlayersToTroupe(args.Players.ToList());
    }

    public void AssignPlayersToTroupe(List<ICommonSession> players)
    {
        foreach (var troupe in GetOrderedTroupes())
        {
            if (players.Count == 0)
                break;
            TryAssignToTroupe(troupe, ref players);
        }

        if (players.Count > 0)
        {
            Log.Warning($"Failed to assign all players to troupes! Leftover count: {players.Count}");
        }
    }

    public bool TryAssignToTroupe(Entity<ESTroupeRuleComponent> ent, ref List<ICommonSession> players)
    {
        var troupe = _prototypeManager.Index(ent.Comp.Troupe);

        var filteredPlayers = players.Where(s => IsPlayerValid(troupe, s)).ToList();

        var playerCount = _esAuditions.GetPlayerCount();
        var targetCount = Math.Clamp(playerCount / ent.Comp.PlayersPerTargetMember, ent.Comp.MinTargetMembers, ent.Comp.MaxTargetMembers);
        var targetDiff = Math.Min(targetCount - ent.Comp.TroupeMemberMinds.Count, filteredPlayers.Count);
        if (targetDiff <= 0)
            return false;

        for (var i = 0; i < targetDiff; i++)
        {
            var player = _random.PickAndTake(filteredPlayers);
            players.Remove(player);

            if (!_mind.TryGetMind(player, out var mind, out var mindComp))
            {
                Log.Warning($"Failed to get mind for session {player}");
                continue;
            }

            if (!TryGetMask((mind, mindComp), troupe, out var mask))
            {
                Log.Warning($"Failed to get mask for session {player} on troupe {troupe.ID} ({ToPrettyString(ent)}");
                continue;
            }

            ApplyMask(ent, (mind, mindComp), mask.Value);
        }
        return true;
    }

    public List<Entity<ESTroupeRuleComponent>> GetOrderedTroupes()
    {
        var troupes = new List<Entity<ESTroupeRuleComponent>>();
        var query = EntityQueryEnumerator<ESTroupeRuleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!_gameTicker.IsGameRuleActive(uid))
                continue;

            troupes.Add((uid, comp));
        }

        troupes.Sort((a, b) =>
        {
            var c = a.Comp.Priority.CompareTo(b.Comp.Priority);
            if (c != 0)
                return c;
            return _random.Next() % 2 == 0 ? -1 : 1; // For members of equal priority, sort them randomly.
        });

        return troupes;
    }

    public bool IsPlayerValid(ESTroupePrototype troupe, ICommonSession player)
    {
        if (!_mind.TryGetMind(player, out var mind, out _))
            return false;

        // BUG: MindTryGetJobId doesn't have a NotNullWhen attribute on the out param.
        if (_job.MindTryGetJobId(mind, out var job) && troupe.ProhibitedJobs.Contains(job!.Value))
            return false;

        if (player.AttachedEntity is null)
            return false;

        return true;
    }

    public bool TryGetMask(Entity<MindComponent> mind, ESTroupePrototype troupe, [NotNullWhen(true)] out ProtoId<ESMaskPrototype>? mask)
    {
        mask = null;

        var weights = new Dictionary<ESMaskPrototype, float>();
        foreach (var maskProto in _prototypeManager.EnumeratePrototypes<ESMaskPrototype>())
        {
            if (maskProto.Abstract)
                continue;

            if (maskProto.Troupe != troupe)
                continue;

            weights.Add(maskProto, maskProto.Weight);
        }

        if (weights.Count == 0)
            return false;

        mask = _random.Pick(weights);
        return true;
    }

    public void ApplyMask(Entity<ESTroupeRuleComponent> troupe, Entity<MindComponent> mind, ProtoId<ESMaskPrototype> maskId)
    {
        var mask = _prototypeManager.Index(maskId);
        _role.MindAddRole(mind, MindRole, mind, true);

        var objectives = _entityTable.GetSpawns(mask.Objectives);
        foreach (var objective in objectives)
        {
            _mind.TryAddObjective(mind, mind, objective);
        }

        var msg = Loc.GetString("es-mask-selected-chat-message",
            ("role", Loc.GetString(mask.Name)),
            ("description", Loc.GetString(mask.Description)));

        if (mind.Comp.UserId is { } userId && _player.TryGetSessionById(userId, out var session))
        {
            _chat.ChatMessageToOne(ChatChannel.Server, msg, msg, default, false, session.Channel, Color.Plum);
        }

        troupe.Comp.TroupeMemberMinds.Add(mind);
    }
}
