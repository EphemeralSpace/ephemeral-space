#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.IntegrationTests.Tests._Citadel.Attributes;
using Content.Server.Mind;
using Content.Shared.Players;
using Robust.Client.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Citadel;

public abstract partial class GameTest
{
    [SidedDependency(Side.Server)] private readonly IGameTiming _sGameTiming = default!;
    [SidedDependency(Side.Client)] private readonly IClientGameTiming _cGameTiming = default!;

    /// <summary>
    ///     Marks the test pair as dirty, ensuring it is returned as such.
    /// </summary>
    public void MarkDirty()
    {
        _pairDirty = true;
    }

    /// <summary>
    ///     Returns a string representation of an entity for the server.
    /// </summary>
    public string SToPrettyString(EntityUid uid)
    {
        return Pair.Server.EntMan.ToPrettyString(uid);
    }

    /// <summary>
    ///     Returns a string representation of an entity for the client.
    /// </summary>
    public string CToPrettyString(EntityUid uid)
    {
        return Pair.Client.EntMan.ToPrettyString(uid);
    }

    /// <summary>
    ///     Converts a server EntityUid into the client-side equivalent entity.
    /// </summary>
    public EntityUid ToClientUid(EntityUid serverUid)
    {
        return Pair.ToClientUid(serverUid);
    }

    /// <summary>
    ///     Converts a client EntityUid into the server-side equivalent entity.
    /// </summary>
    public EntityUid ToServerUid(EntityUid clientUid)
    {
        return Pair.ToServerUid(clientUid);
    }

    /// <summary>
    ///     Retrieves the given component from an entity on the server.
    /// </summary>
    public T SComp<T>(EntityUid target)
        where T : IComponent
    {
        return SEntMan.GetComponent<T>(target);
    }

    /// <summary>
    ///     Attempts to retrieve the given component from an entity on the server.
    /// </summary>
    public bool STryComp<T>(EntityUid? target, [NotNullWhen(true)] out T? component)
        where T : IComponent
    {
        return SEntMan.TryGetComponent(target, out component);
    }

    /// <summary>
    ///     Retrieves the given component from an entity on the client.
    /// </summary>
    public T CComp<T>(EntityUid target)
        where T : IComponent
    {
        return CEntMan.GetComponent<T>(target);
    }

    /// <summary>
    ///     Attempts to retrieve the given component from an entity on the server.
    /// </summary>
    public bool CTryComp<T>(EntityUid? target, [NotNullWhen(true)] out T? component)
        where T : IComponent
    {
        return SEntMan.TryGetComponent(target, out component);
    }

    /// <summary>
    ///     Pairs an EntityUid with the given component, from the server.
    /// </summary>
    public Entity<T> SEntity<T>(EntityUid target)
        where T : IComponent
    {
        return new(target, SEntMan.GetComponent<T>(target));
    }

    /// <summary>
    ///     Pairs an EntityUid with the given component, from the client.
    /// </summary>
    public Entity<T> CEntity<T>(EntityUid target)
        where T : IComponent
    {
        return new(target, CEntMan.GetComponent<T>(target));
    }

    /// <summary>
    ///     Spawns an entity on the server.
    /// </summary>
    /// <remarks>This tracks the entity for post-test cleanup.</remarks>
    public EntityUid SSpawn(string? id)
    {
        var res = SEntMan.Spawn(id);
        _serverEntitiesToClean.Add(res);
        return res;
    }

    /// <summary>
    ///     Spawns an entity on the server at a location.
    /// </summary>
    /// <remarks>This tracks the entity for post-test cleanup.</remarks>
    public EntityUid SSpawnAtPosition(string? id, EntityCoordinates coordinates)
    {
        var res = SEntMan.SpawnAtPosition(id, coordinates);
        _serverEntitiesToClean.Add(res);
        return res;
    }

    /// <summary>
    ///     Spawns an entity on the client.
    /// </summary>
    /// <remarks>This tracks the entity for post-test cleanup.</remarks>
    public EntityUid CSpawn(string? id)
    {
        var res = CEntMan.Spawn(id);
        _clientEntitiesToClean.Add(res);
        return res;
    }

    /// <summary>
    ///     Spawns an entity on the server at a location.
    /// </summary>
    /// <remarks>This tracks the entity for post-test cleanup.</remarks>
    public EntityUid CSpawnAtPosition(string? id, EntityCoordinates coordinates)
    {
        var res = CEntMan.SpawnAtPosition(id, coordinates);
        _clientEntitiesToClean.Add(res);
        return res;
    }

    /// <summary>
    ///     Asynchronously spawns an entity on the server.
    /// </summary>
    /// <returns></returns>
    public async Task<EntityUid> Spawn(string? id)
    {
        var ent = EntityUid.Invalid;

        await Server.WaitPost(() => ent = SSpawn(id));

        return ent;
    }

    /// <summary>
    ///     Asynchronously spawns an entity on the server at the given position.
    /// </summary>
    /// <returns></returns>
    public async Task<EntityUid> SpawnAtPosition(string? id, EntityCoordinates coords)
    {
        var ent = EntityUid.Invalid;

        await Server.WaitPost(() => ent = SSpawnAtPosition(id, coords));

        return ent;
    }


    /// <summary>
    ///     Deletes an entity on the server immediately.
    /// </summary>
    public void SDeleteNow(EntityUid id)
    {
        SEntMan.DeleteEntity(id);
    }

    /// <summary>
    ///     Deletes an entity on the client immediately.
    /// </summary>
    public void CDeleteNow(EntityUid id)
    {
        CEntMan.DeleteEntity(id);
    }

    /// <summary>
    ///     Queues an entity for deletion at the end of the tick on the server.
    /// </summary>
    public void SQueueDel(EntityUid id)
    {
        SEntMan.QueueDeleteEntity(id);
    }

    /// <summary>
    ///     Queues an entity for deletion at the end of the tick on the client.
    /// </summary>
    public void CQueueDel(EntityUid id)
    {
        CEntMan.QueueDeleteEntity(id);
    }

    /// <summary>
    ///     Fully loads a given map on the server, optionally initializing it, and runs the pair in sync for a few ticks
    ///     to ensure both sides have fully loaded the map.
    /// </summary>
    /// <remarks>
    ///     The test map is global to the game test and is exposed through the TestMap property when ready. Cleanup is
    ///     handled automatically as well.
    /// </remarks>
    [MemberNotNull(nameof(TestMap))]
    public async Task<TestMapData> LoadTestMap(ResPath mapPath, bool initialized = true)
    {
        // C# is smart, but not that smart, we need to make a promise here.

        await Pair.LoadTestMap(mapPath, initialized);
        await RunUntilSynced();

#pragma warning disable CS8774
        return TestMap!;
#pragma warning restore

    }

    [MemberNotNull(nameof(TestMap))]
    public async Task CreateTestMap(TestMapMode kind, bool initialized = true)
    {
        if (TestMap is not null)
            throw new NotSupportedException("Dismantle your existing TestMap before creating a new one.");

        switch (kind)
        {
            case TestMapMode.None:
                break;
            case TestMapMode.Basic:
                await Pair.CreateTestMap(initialized);
                break;
            case TestMapMode.Arena:
                await Pair.CreateTestMap(initialized);
                await FillTestMapArena();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }

        await RunUntilSynced();

        // C# is smart, but not that smart, we need to make a promise here.
#pragma warning disable CS8774
        // ReSharper disable once RedundantJumpStatement
        return;
#pragma warning restore
    }


    /// <summary>
    ///     Runs the client and server for the given number of ticks, in lockstep.
    /// </summary>
    /// <remarks>Do not use this as a barrier for client-server synchronization, use <see cref="RunUntilSynced"/>.</remarks>
    public Task RunTicksSync(int ticks)
    {
        return Pair.RunTicksSync(ticks);
    }

    /// <summary>
    ///     Runs the pairs just long enough for PVS to send entities, ensuring the client's current tick is what the server's was at call time.
    /// </summary>
    public async Task RunUntilSynced()
    {
        var startTime = _sGameTiming.CurTick;

        while (_cGameTiming.LastRealTick < startTime)
        {
            await RunTicksSync(1);
        }
    }

    /// <summary>
    /// Convert a time interval to some number of ticks.
    /// </summary>
    public int SecondsToTicks(float seconds)
    {
        return (int) Math.Ceiling(seconds / Server.Timing.TickPeriod.TotalSeconds);
    }

    /// <summary>
    ///     Runs the test pair for a number of (simulated) seconds.
    /// </summary>
    /// <remarks>
    ///     Does not actually take N seconds to evaluate, the game ticks as fast as possible.
    ///     Do not use this as a barrier for client-server synchronization, use <see cref="RunUntilSynced"/>.
    /// </remarks>
    public Task RunSeconds(float seconds)
    {
        return RunTicksSync(SecondsToTicks(seconds));
    }

    /// <inheritdoc cref="M:Robust.Shared.GameObjects.EntityManager.EntityQueryEnumerator``1"/>
    public EntityQueryEnumerator<T> SQuery<T>()
        where T : IComponent
    {
        return Server.EntMan.EntityQueryEnumerator<T>();
    }

    /// <inheritdoc cref="M:Robust.Shared.GameObjects.EntityManager.EntityQueryEnumerator``1"/>
    public EntityQueryEnumerator<T> CQuery<T>()
        where T : IComponent
    {
        return Client.EntMan.EntityQueryEnumerator<T>();
    }

    /// <summary>
    ///     Tests whether any entity exists with the given component on the server.
    /// </summary>
    public bool SAnyExists<T>()
        where T : IComponent
    {
        var query = SQuery<T>();

        return query.MoveNext(out _);
    }

    /// <summary>
    ///     Tests whether any entity exists with the given component on the client.
    /// </summary>
    public bool CAnyExists<T>()
        where T : IComponent
    {
        var query = CQuery<T>();

        return query.MoveNext(out _);
    }

    /// <summary>
    ///     Queries the number of entities with a given component on the server.
    /// </summary>
    public int SQueryCount<T>()
        where T : IComponent
    {
        return Server.EntMan.Count<T>();
    }

    /// <summary>
    ///     Queries every entity with the given component on the server and returns a list of them.
    /// </summary>
    public List<Entity<T>> SQueryList<T>()
        where T : IComponent
    {
        var list = new List<Entity<T>>(SQueryCount<T>());

        var q = SQuery<T>();

        while (q.MoveNext(out var ent, out var comp1))
        {
            list.Add((ent, comp1));
        }

        return list;
    }

    /// <summary>
    ///     Queries every entity with the given component on the server and returns a list of them.
    /// </summary>
    public List<Entity<T>> CQueryList<T>()
        where T : IComponent
    {
        var list = new List<Entity<T>>(CQueryCount<T>());

        var q = CQuery<T>();

        while (q.MoveNext(out var ent, out var comp1))
        {
            list.Add((ent, comp1));
        }

        return list;
    }

    /// <summary>
    ///     Queries the number of entities with a given component on the client.
    /// </summary>
    public int CQueryCount<T>()
        where T : IComponent
    {
        return Client.EntMan.Count<T>();
    }

    /// <summary>
    ///     Gets a single instance of an entity with the given component on the server, asserting it is the only one.
    /// </summary>
    public bool SQuerySingle<T>([NotNullWhen(true)] out Entity<T>? ent)
        where T : IComponent
    {
        var query = SQuery<T>();

        if (query.MoveNext(out var eid, out var comp))
        {
            Assert.That(query.MoveNext(out var extra, out _),
                Is.False,
                $"Expected only one entity with {typeof(T)}, found {SToPrettyString(eid)} and then {SToPrettyString(extra)}");
            ent = (eid, comp);
            return true;
        }

        ent = null;
        return false;
    }

    /// <summary>
    ///     Gets a single instance of an entity with the given component on the client, asserting it is the only one.
    /// </summary>
    public bool CQuerySingle<T>([NotNullWhen(true)] out Entity<T>? ent)
        where T : IComponent
    {
        var query = CQuery<T>();

        if (query.MoveNext(out var eid, out var comp))
        {
            Assert.That(query.MoveNext(out var extra, out _),
                Is.False,
                $"Expected only one entity with {typeof(T)}, found {CToPrettyString(eid)} and then {CToPrettyString(extra)}");
            ent = (eid, comp);
            return true;
        }

        ent = null;
        return false;
    }

    public async Task<ICommonSession[]> AddDummySessionsSync(int count = 1)
    {
        var res = await Server.AddDummySessions(count);

        await Pair.ReallyBeIdle(); // That takes a while.

        return res;
    }

    /// <summary>
    ///     Checks whether the given entity has been deleted on the server.
    /// </summary>
    public bool SDeleted(EntityUid? ent)
    {
        return Server.EntMan.Deleted(ent);
    }

    /// <summary>
    ///     Checks whether the given entity has been deleted on the client.
    /// </summary>
    public bool CDeleted(EntityUid? ent)
    {
        return Client.EntMan.Deleted(ent);
    }

    /// <summary>
    ///     Checks whether the given entity has the given component.
    /// </summary>
    public bool SHasComp<T>(EntityUid? ent)
        where T : IComponent
    {
        return Server.EntMan.HasComponent<T>(ent);
    }

    /// <summary>
    ///     Checks whether the given entity has the given component.
    /// </summary>
    public bool CHasComp<T>(EntityUid? ent)
        where T : IComponent
    {
        return Client.EntMan.HasComponent<T>(ent);
    }


    /// <summary>
    ///     Assigns the player a body in the test map, ensuring they have a mind as well.
    /// </summary>
    public async Task<EntityUid> AssignPlayerBody(ICommonSession session,
        string playerPrototype = "MobHuman",
        bool godMode = true)
    {
        EntityUid res = default;

        var mindSys = SEntMan.System<MindSystem>();

        await Server.WaitAssertion(() =>
        {
            Assert.That(TestMap,
                Is.Not.Null,
                $"{nameof(AssignPlayerBody)} doesn't work without a {nameof(TestMap)}.");

            // InteractionTest cargo cult.
            mindSys.WipeMind(session.ContentData()?.Mind);

            res = SEntMan.SpawnAtPosition(playerPrototype, TestMap.GridCoords);

            mindSys.ControlMob(session.UserId, res);
        });

        // Sync them back up.
        await Pair.RunTicksSync(5);

        return res;
    }
}
