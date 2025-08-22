using System.Diagnostics.CodeAnalysis;
using Content.Server._ES.Arrivals;
using Content.Server._ES.Masks.Components;
using Content.Server.Chat.Managers;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared._ES.Masks;
using Content.Shared.Chat;
using Content.Shared.EntityTable;
using Content.Shared.Mind;
using Content.Shared.Random.Helpers;
using Content.Shared.Roles.Components;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.Masks;

public sealed class ESMaskSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly RoleSystem _role = default!;

    private static readonly EntProtoId<ESMaskRoleComponent> MindRole = "ESMindRoleMask";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESPlayersArrivedEvent>(OnPlayersArrived);
    }

    private void OnPlayersArrived(ref ESPlayersArrivedEvent ev)
    {
        foreach (var player in ev.Players)
        {
            if (!_mind.TryGetMind(player, out var mindUid, out var mindComp))
                continue;

            if (!TryGetMask((mindUid, mindComp), out var mask))
                continue;

            ApplyMask((mindUid, mindComp), mask.Value);
        }
    }

    public bool TryGetMask(Entity<MindComponent> mind, [NotNullWhen(true)] out ProtoId<ESMaskPrototype>? mask)
    {
        mask = null;

        var weights = new Dictionary<ESMaskPrototype, float>();
        foreach (var maskProto in _prototypeManager.EnumeratePrototypes<ESMaskPrototype>())
        {
            if (maskProto.Abstract)
                continue;

            if (maskProto.BlockNonAntagJobs)
            {
                if (_role.MindHasRole<JobRoleComponent>(mind.AsNullable(), out var role) &&
                    role.Value.Comp1.JobPrototype is { } job)
                {
                    if (!_prototypeManager.Index(job).CanBeAntag)
                        continue;
                }
            }

            weights.Add(maskProto, maskProto.Weight);
        }

        if (weights.Count == 0)
            return false;

        mask = _random.Pick(weights);
        return true;
    }

    public void ApplyMask(Entity<MindComponent> mind, ProtoId<ESMaskPrototype> maskId)
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
    }
}
