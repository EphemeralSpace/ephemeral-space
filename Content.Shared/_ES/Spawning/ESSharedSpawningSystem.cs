using System.Diagnostics.CodeAnalysis;
using Content.Shared._ES.CCVar;
using Content.Shared._ES.Spawning.Components;
using Content.Shared.Administration.Managers;
using Content.Shared.Ghost;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Spawning;

/// <summary>
/// Handles specific logic related to respawning
/// </summary>
public abstract class ESSharedSpawningSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminManager _admin = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedPvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    protected bool RespawnsEnabled;
    protected TimeSpan RespawnDelay;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<MindRemovedMessage>(OnMindRemoved);

        Subs.CVar(_config, ESCVars.ESRespawnEnabled, v => RespawnsEnabled = v, true);
        Subs.CVar(_config, ESCVars.ESRespawnDelay, d => RespawnDelay = TimeSpan.FromSeconds(d), true);
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (ev.NewMobState != MobState.Dead)
            return;

        if (!_player.TryGetSessionByEntity(ev.Target, out var session))
            return;

        ResetRespawnTimer(session);
    }

    private void OnMindRemoved(MindRemovedMessage ev)
    {
        if (!_player.TryGetSessionById(ev.Mind.Comp.UserId, out var session))
            return;

        if (TryComp<MobStateComponent>(ev.Container, out var mobState) && _mobState.IsDead(ev.Container, mobState))
            return;

        if (HasComp<GhostComponent>(ev.Container))
            return;

        ResetRespawnTimer(session);
    }

    public void ResetRespawnTimer(ICommonSession session)
    {
        if (!TryGetRespawnTracker(out var tracker))
            return;

        var delay = _admin.IsAdmin(session) ? TimeSpan.Zero : RespawnDelay;

        var comp = tracker.Value.Comp;
        comp.RespawnTimes.GetOrNew(session.UserId);
        comp.RespawnTimes[session.UserId] = Timing.CurTime + delay;
        Dirty(tracker.Value);
    }

    [PublicAPI]
    public TimeSpan GetRespawnTime(ICommonSession session)
    {
        if (!TryGetRespawnTracker(out var tracker))
            return TimeSpan.Zero;

        if (!tracker.Value.Comp.RespawnTimes.TryGetValue(session.UserId, out var respawnTime))
            return TimeSpan.Zero;

        return respawnTime;
    }

    public virtual bool TryGetRespawnTracker([NotNullWhen(true)] out Entity<ESRespawnTrackerComponent>? respawnTracker)
    {
        respawnTracker = null;
        if (!RespawnsEnabled)
            return false;

        var query = EntityQueryEnumerator<ESRespawnTrackerComponent>();
        while (query.MoveNext(out var u1, out var c1))
        {
            respawnTracker = (u1, c1);
            return true;
        }

        if (_net.IsClient)
            return false;

        var uid = Spawn();
        var comp = AddComp<ESRespawnTrackerComponent>(uid);
        _pvsOverride.AddGlobalOverride(uid);
        respawnTracker = (uid, comp);
        return true;
    }
}

[Serializable, NetSerializable]
public sealed class ESSpawnPlayerEvent(List<NetEntity> stations, ProtoId<JobPrototype> jobId) : EntityEventArgs
{
    public List<NetEntity> Stations = stations;
    public ProtoId<JobPrototype> JobId = jobId;
}
