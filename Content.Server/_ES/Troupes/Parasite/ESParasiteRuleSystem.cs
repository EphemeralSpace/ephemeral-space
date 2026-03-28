using Content.Server._ES.Masks;
using Content.Server._ES.Objectives;
using Content.Server._ES.Troupes.Parasite.Components;
using Content.Server.Chat.Managers;
using Content.Server.RoundEnd;
using Content.Server.Speech.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.Objectives.Components;
using Content.Shared.Administration.Systems;
using Content.Shared.Chat;
using Content.Shared.Mind;
using Robust.Server.Player;

namespace Content.Server._ES.Troupes.Parasite;

public sealed class ESParasiteRuleSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ESEntityTimerSystem _entityTimer = default!;
    [Dependency] private readonly ESMaskSystem _mask = default!;
    [Dependency] private readonly ESObjectiveSystem _objective = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly VocalSystem _vocal = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESObjectiveProgressChangedEvent>(OnProgressChanged);
    }

    private void OnProgressChanged(ref ESObjectiveProgressChangedEvent args)
    {
        var query = EntityQueryEnumerator<ESParasiteRuleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!_objective.HasObjective(uid, args.Objective))
                continue;

            if (comp.ObjectivesCompleted)
                continue;

            if (!_objective.AllCompleted(uid))
                continue;

            StartEndPhase((uid, comp));
        }
    }

    private void StartEndPhase(Entity<ESParasiteRuleComponent> ent)
    {
        ent.Comp.ObjectivesCompleted = true;

        var msg = Loc.GetString("es-parasite-swarm-notif");
        var wrappedMsg = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        foreach (var mind in _mask.GetTroupeMembers(ent.Owner))
        {
            if (!TryComp<MindComponent>(mind, out var mindComp) ||
                !_playerManager.TryGetSessionById(mindComp.UserId, out var session))
                continue;

            _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMsg, default, false, session.Channel, Color.YellowGreen);
        }

        _entityTimer.SpawnMethodTimer(ent.Comp.SwarmDelay, () => { TransformTroupeMembers(ent); });
        _entityTimer.SpawnMethodTimer(ent.Comp.WinDelay, () => { ent.Comp.WinStarted = true; });
    }

    private void TransformTroupeMembers(Entity<ESParasiteRuleComponent> ent)
    {
        foreach (var mind in _mask.GetTroupeMembers(ent.Owner))
        {
            if (!TryComp<MindComponent>(mind, out var mindComp ) ||
                mindComp.OwnedEntity is not { } owned)
                continue;

            _rejuvenate.PerformRejuvenate(ent);
            _stationSpawning.EquipStartingGear(owned, ent.Comp.SwarmGear);
            _vocal.TryPlayScreamSound(ent.Owner);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ESParasiteRuleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.WinStarted)
                continue;

            if (_objective.AllCompleted(uid))
                _roundEnd.EndRound();
        }
    }
}
