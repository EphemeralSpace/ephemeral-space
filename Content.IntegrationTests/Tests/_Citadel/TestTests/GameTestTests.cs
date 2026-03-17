#nullable enable
using System.Collections.Generic;
using System.Threading;
using Content.IntegrationTests.Tests._Citadel.Attributes;
using Content.IntegrationTests.Tests._Citadel.Constraints;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Citadel.TestTests;

public sealed class GameTestTests : GameTest
{
    [SidedDependency(Side.Server)] private readonly IEntityManager _sEntMan = default!;
    [SidedDependency(Side.Client)] private readonly IEntityManager _cEntMan = default!;

    [Test]
    [Description("Runs a game test and ticks it a bit.")]
    public async Task GameTestRuns()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Server, Is.Not.Null);
            Assert.That(Client, Is.Not.Null);
        }

        Server.RunTicks(2);
        Client.RunTicks(2);

        await Server.WaitIdleAsync();
        await Client.WaitIdleAsync();
    }

    [Test]
    [Description("Asserts that sided dependencies actually grab from the right sides.")]
    public void DependenciesRespectSides()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(!ReferenceEquals(_sEntMan, _cEntMan), "server and client entity managers should be distinct");
            Assert.That(_sEntMan, Is.EqualTo(SEntMan).Using<object?>(ReferenceEqualityComparer.Instance));
            Assert.That(_cEntMan, Is.EqualTo(CEntMan).Using<object?>(ReferenceEqualityComparer.Instance));
        }
    }

    [Test]
    [Description("Tests that RunOnSide actually does as expected.")]
    [RunOnSide(Side.Server)]
    public void TestServerSide()
    {
        Assert.That(Thread.CurrentThread, Is.EqualTo(ServerThread));
    }

    [Test]
    [Description("Tests that RunOnSide actually does as expected.")]
    [RunOnSide(Side.Client)]
    public void TestClientSide()
    {
        Assert.That(Thread.CurrentThread, Is.EqualTo(ClientThread));
    }

    [Test]
    [Description("Assert that the data scrounger finds prototypes by type.")]
    public void ScroungeByType()
    {
        var scrounged = PrototypeDataScrounger.PrototypesOfKind<EntityPrototype>();
        Assert.That(scrounged, Is.Not.Empty);
    }

    [Test]
    [Description("Assert that RunUntilSynced waits long enough for the client and server to actually sync.")]
    [TestMap(TestMapMode.Arena)]
    public async Task SyncRunsLongEnough()
    {
        // Bring some friends we're gonna stress PVS.
        await SpawnAtPosition("MobHuman", TestMap!.GridCoords);
        await SpawnAtPosition("MobHuman", TestMap.GridCoords);
        await SpawnAtPosition("MobHuman", TestMap.GridCoords);
        var bigEntity = await SpawnAtPosition("MobHuman", TestMap.GridCoords);
        await RunUntilSynced();

        Assert.That(ToClientUid(bigEntity), Is.Initialized(Client));
    }

    [Test]
    [Description("Ensure that you cannot create a test map twice without dismantling the old one.")]
    public async Task EnsureNoAccidentalMapOverrides()
    {
        await CreateTestMap(TestMapMode.Basic);

        Assert.CatchAsync<NotSupportedException>(async () =>
        {
            await CreateTestMap(TestMapMode.Basic);
        });
    }

    [Test]
    [Description("Ensure that TestMapAttribute actually makes a map.")]
    [TestOf(typeof(TestMapAttribute))]
    [TestMap(TestMapMode.Arena)]
    public void EnsureTestMapAttributeFunctions()
    {
        Assert.That(TestMap, Is.Not.Null);
    }
}
