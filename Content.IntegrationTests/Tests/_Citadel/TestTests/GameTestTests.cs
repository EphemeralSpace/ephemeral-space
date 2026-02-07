#nullable enable
using System.Collections.Generic;
using System.Threading;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Citadel.TestTests;

[Explicit(reason: """
    These tests will all be reproduced by other tests, like ConstraintsTests, failing.
    They exist more as a debugging aid than anything else and to catch certain mistakes.
    As such, they're not part of the normal test run, but people modifying GameTest are
    expected to run these!
    """)]
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
}
