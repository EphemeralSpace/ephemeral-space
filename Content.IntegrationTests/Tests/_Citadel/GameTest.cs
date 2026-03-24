#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Content.IntegrationTests.Pair;
using Content.IntegrationTests.Tests._Citadel.Attributes;
using Content.IntegrationTests.Tests._Citadel.Constraints;
using NUnit.Framework.Internal;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests._Citadel;

/// <summary>
/// <para>
///     A test fixture with an integrated <see cref="GameTest.Pair">test pair</see>,
///     proxy methods for efficient test writing, utilities for ensuring tests clean up correctly,
///     <see cref="TestMapAttribute"> configurable test map management</see>,
///     <see cref="TestPlayer">player puppeteering</see>, and dependency injection
///     (<see cref="SystemAttribute"/> and <see cref="SidedDependencyAttribute"/>).
/// </para>
/// <para>
///     Tests using GameTest support some additional attributes, namely <see cref="RunOnSideAttribute"/> and
///     <see cref="TestMapAttribute"/>. Attributes can be used to control how the test runs.
/// </para>
/// </summary>
/// <seealso cref="CompConstraintExtensions"/>
/// <seealso cref="LifeStageConstraintExtensions"/>
[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public abstract partial class GameTest
{
    private bool _pairDirty;

    private readonly List<EntityUid> _serverEntitiesToClean = new();
    private readonly List<EntityUid> _clientEntitiesToClean = new();

    /// <summary>
    ///     Tests-testing-tests assistant to run right before the pair is returned.
    /// </summary>
    public event Action? PreFinalizeHook;

    public Thread ServerThread { get; private set; } = default!;
    public Thread ClientThread { get; private set; } = default!;

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
    public ICommonSession? ServerSession => Pair.Player;

    /// <summary>
    ///     The server-side entity manager.
    /// </summary>
    public IEntityManager SEntMan => Server.EntMan;

    /// <summary>
    ///     The client-side entity manager.
    /// </summary>
    public IEntityManager CEntMan => Client.EntMan;

    /// <summary>
    ///     The test map we're using, if any.
    /// </summary>
    public TestMapData? TestMap => Pair.TestMap;

    [SetUp]
    public virtual async Task DoSetup()
    {
        _pairDirty = false;
        Pair = await PoolManager.GetServerClient(PoolSettings);

        Task.WaitAll(
            Server.WaitPost(() =>
            {
                ServerThread = Thread.CurrentThread;
            }),
            Client.WaitPost(() => ClientThread = Thread.CurrentThread)
        );

        InjectDependencies(this);

        var test = TestContext.CurrentContext.Test;

        var attribs = test.Method!.GetCustomAttributes<IGameTestModifier>(false);
        var suiteAttribs = test.Method!.TypeInfo.GetCustomAttributes<IGameTestModifier>(true);

        foreach (var attribute in attribs.Concat(suiteAttribs))
        {
            await attribute.ApplyToTest(this);
        }

        await DoPreTestOverrides();
    }

    public void InjectDependencies(object target)
    {
        foreach (var field in target.GetType().GetAllFields())
        {
            if (field.GetCustomAttribute<SystemAttribute>() is { } sysAttrib)
            {
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (sysAttrib.Side is Side.Server)
                {
                    field.SetValue(target, Server.EntMan.EntitySysManager.GetEntitySystem(field.FieldType));
                }
                else
                {
                    field.SetValue(target, Client.EntMan.EntitySysManager.GetEntitySystem(field.FieldType));
                }
            }
            else if (field.GetCustomAttribute<SidedDependencyAttribute>() is { } depAttrib)
            {
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (depAttrib.Side is Side.Server)
                {
                    field.SetValue(target, Server.InstanceDependencyCollection.ResolveType(field.FieldType));
                }
                else
                {
                    field.SetValue(target, Client.InstanceDependencyCollection.ResolveType(field.FieldType));
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
            await RunUntilSynced();

            RestoreCVars();

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
            PreFinalizeHook?.Invoke();

            if (!_pairDirty)
                await Pair.CleanReturnAsync();
            else
                await Pair.DisposeAsync();
        }
    }
}

/// <summary>
///     Possible configurations for <see cref="GameTest.TestMapSetting"/>.
/// </summary>
/// <seealso cref="TestMapMode.None"/>
/// <seealso cref="TestMapMode.Basic"/>
/// <seealso cref="TestMapMode.Arena"/>
/// <seealso cref="GameTest"/>
public enum TestMapMode
{
    // REMARK: IF you add new modes suitable for TestPlayer, make sure to add them to SitAroundInnocently.

    /// <summary>
    ///     Indicates no testmap should be loaded.
    /// </summary>
    None,
    /// <summary>
    ///     Indicates a single tile, empty map should be loaded.
    ///     Atmospherics and gravity are not configured.
    /// </summary>
    Basic,
    /// <summary>
    ///     Indicates a larger 9x9 "arena" map should be created,
    ///     with atmos and gravity set up. This is useful alongside <see cref="TestPlayer"/>
    ///     for tests that need to puppeteer a player.
    /// </summary>
    Arena,
}
