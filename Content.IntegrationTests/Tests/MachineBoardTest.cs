using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Construction.Components;
using Content.Shared.Construction.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests;

public sealed class MachineBoardTest : GameTest
{
    /// <summary>
    /// A list of machine boards that can be ignored by this test.
    /// </summary>
    private readonly HashSet<string> _ignoredPrototypes = new()
    {
    };

    /// <summary>
    /// Ensures that every single machine board's corresponding entity
    /// is a machine and can be properly deconstructed.
    /// </summary>
    [Test]
    public async Task TestMachineBoardHasValidMachine()
    {
        var pair = Pair;
        var server = pair.Server;

        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var compFact = server.ResolveDependency<IComponentFactory>();

        await server.WaitAssertion(() =>
        {
            foreach (var p in protoMan.EnumeratePrototypes<EntityPrototype>()
                         .Where(p => !p.Abstract)
                         .Where(p => !pair.IsTestPrototype(p))
                         .Where(p => !_ignoredPrototypes.Contains(p.ID)))
            {
                if (!p.TryGetComponent<MachineBoardComponent>(out var mbc, compFact))
                    continue;
                var mId = mbc.Prototype;

                Assert.Multiple(() =>
                {
                    Assert.That(protoMan.TryIndex<EntityPrototype>(mId, out var mProto),
                        $"Machine board {p.ID}'s corresponding machine has an invalid prototype.");
                    Assert.That(mProto.TryGetComponent<MachineComponent>(out var mComp, compFact),
                        $"Machine board {p.ID}'s corresponding machine {mId} does not have MachineComponent");
                    Assert.That(mComp.Board, Is.EqualTo(p.ID),
                        $"Machine {mId}'s BoardPrototype is not equal to it's corresponding machine board, {p.ID}");
                });
            }
        });
    }

    /// <summary>
    /// Ensures that every single computer board's corresponding entity
    /// is a computer that can be properly deconstructed to the correct board
    /// </summary>
    [Test]
    public async Task TestValidateBoardComponentRequirements()
    {
        var pair = Pair;
        var server = pair.Server;

        var entMan = server.ResolveDependency<IEntityManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            foreach (var p in protoMan.EnumeratePrototypes<EntityPrototype>()
                         .Where(p => !p.Abstract)
                         .Where(p => !pair.IsTestPrototype(p))
                         .Where(p => !_ignoredPrototypes.Contains(p.ID)))
            {
                if (!p.TryGetComponent<MachineBoardComponent>(out var board, entMan.ComponentFactory))
                    continue;

                Assert.Multiple(() =>
                {
                    foreach (var component in board.ComponentRequirements.Keys)
                    {
                        Assert.That(entMan.ComponentFactory.TryGetRegistration(component, out _), $"Invalid component requirement {component} specified on machine board entity {p}");
                    }
                });
            }
        });
    }
}
