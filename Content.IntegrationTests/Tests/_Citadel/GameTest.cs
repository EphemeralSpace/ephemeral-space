#nullable enable
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Content.IntegrationTests.Pair;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests._Citadel;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public abstract partial class GameTest
{
    private bool _pairDirty;

    private readonly List<EntityUid> _serverEntitiesToClean = new();
    private readonly List<EntityUid> _clientEntitiesToClean = new();

    private protected Thread ServerThread = default!;
    private protected Thread ClientThread = default!;

    /// <summary>
    ///     Settings for the client/server pair. By default, this gets you a client and server that have connected together.
    /// </summary>
    public virtual PoolSettings PoolSettings => new() { Connected = true };

    /// <summary>
    ///     The client and server pair.
    /// </summary>
    public TestPair Pair { get; private set; } = default!; // NULLABILITY: This is always set during test setup.

    /// <summary>
    ///     The game server instance.
    /// </summary>
    public RobustIntegrationTest.ServerIntegrationInstance Server => Pair.Server;

    /// <summary>
    ///     The game client instance.
    /// </summary>
    public RobustIntegrationTest.ClientIntegrationInstance Client => Pair.Client;

    /// <summary>
    ///     The test player's server session, if any.
    /// </summary>
    public ICommonSession? Player => Pair.Player;

    /// <summary>
    ///     The server-side entity manager.
    /// </summary>
    public IEntityManager SEntMan => Server.EntMan;

    /// <summary>
    ///     The client-side entity manager.
    /// </summary>
    public IEntityManager CEntMan => Client.EntMan;

    [SetUp]
    public virtual async Task DoSetup()
    {
        _pairDirty = false;
        Pair = await PoolManager.GetServerClient(PoolSettings);

        Task.WaitAll(
            Server.WaitPost(() => ServerThread = Thread.CurrentThread),
            Client.WaitPost(() => ClientThread = Thread.CurrentThread)
        );


        foreach (var field in GetType().GetAllFields())
        {
            if (field.GetCustomAttribute<SystemAttribute>() is { } sysAttrib)
            {
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (sysAttrib.Side is Side.Server)
                {
                    field.SetValue(this, Server.EntMan.EntitySysManager.GetEntitySystem(field.FieldType));
                }
                else
                {
                    field.SetValue(this, Client.EntMan.EntitySysManager.GetEntitySystem(field.FieldType));
                }
            }
            else if (field.GetCustomAttribute<SidedDependencyAttribute>() is { } depAttrib)
            {
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (depAttrib.Side is Side.Server)
                {
                    field.SetValue(this, Server.InstanceDependencyCollection.ResolveType(field.FieldType));
                }
                else
                {
                    field.SetValue(this, Client.InstanceDependencyCollection.ResolveType(field.FieldType));
                }
            }
        }
    }

    [TearDown]
    public async Task DoTeardown()
    {
        try
        {
            // Roll forward til sync for teardown.
            await SyncTicks(1);

            await Server.WaitAssertion(() =>
            {
                foreach (var junk in _serverEntitiesToClean)
                {
                    if (!SEntMan.Deleted(junk))
                        SEntMan.DeleteEntity(junk);
                }
            });

            await Client.WaitAssertion(() =>
            {
                foreach (var junk in _clientEntitiesToClean)
                {
                    if (!CEntMan.Deleted(junk))
                        CEntMan.DeleteEntity(junk);
                }
            });
        }
        catch (Exception)
        {
            _pairDirty = true;
            throw;
        }
        finally
        {
            if (!_pairDirty)
                await Pair.CleanReturnAsync();
            else
                await Pair.DisposeAsync();
        }
    }
}
