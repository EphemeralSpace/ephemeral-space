using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Tests._Citadel;
using Content.Server._ES.Masks.Masquerades;
using Content.Server.GameTicking;
using Content.Shared._Citadel.Utilities;
using Content.Shared._ES.Masks;
using Content.Shared._ES.Masks.Components;
using Content.Shared._ES.Masks.Masquerades;
using Content.Shared.GameTicking;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;

namespace Content.IntegrationTests.Tests._ES.Masquerades;

[TestFixture]
public sealed class MasqueradeTests
{
    [GameTest(RunOnSide = Side.Server)]
    public void TestMasqueradeSerialization(
        [SidedDependency(Side.Server)] ISerializationManager ser,
        [SidedDependency(Side.Server)] IPrototypeManager proto
        )
    {
        // We don't support this yet, so whichever shmuck adds it is gonna need to write tests.
        // My advice is to do a round-trip test where you deserialize entries, then reserialize them and verify they're string equal.
        Assert.Throws<NotImplementedException>(() =>
        {
            ser.WriteValue<MasqueradeEntry>(
                new MasqueradeEntry.DirectEntry(new HashSet<ProtoId<ESMaskPrototype>>() { "Foo", "Bar" }, 1, false));
        });
    }

    [GameTest(RunOnSide = Side.Server, Description = "Ensures that selecting masks from MasqueradeEntry is deterministic.")]
    public void MasqueradeEntryDeterminism(
        [SidedDependency(Side.Server)] IPrototypeManager proto,
        [SidedDependency(Side.Server)] IRobustRandom globalRng
    )
    {
        void TestOnEntry(MasqueradeEntry entry)
        {
            // This can in theory flake so let's try a few times.
            for (var i = 0; i < 100; i++)
            {
                var rngSeed = new RngSeed(globalRng);
                var rng1 = rngSeed.IntoRandomizer();
                var rng2 = rngSeed.IntoRandomizer();

                var masks1 = entry!.PickMasks(rng1, proto);
                var masks2 = entry!.PickMasks(rng2, proto);

                Assert.That(masks1, Is.EqualTo(masks2), $"Expected two calls from the same seed to be identical, and they weren't. Seed is {rngSeed.ToString()}");
            }
        }

        {
            MasqueradeEntry.TryRead("Foo/Bar/Baz", null, out var entry, out var error);

            Assert.That(error, Is.Null);

            TestOnEntry(entry);
        }

        {
            MasqueradeEntry.TryRead("#Freaks", null, out var entry, out var error);

            Assert.That(error, Is.Null);

            TestOnEntry(entry);
        }
    }

    [GameTest(RunOnSide = Side.Server, Description = "Ensures that masquerade mask selection itself is deterministic.")]
    public void MasqueradeDeterminism(
        [SidedDependency(Side.Server)] IPrototypeManager proto,
        [SidedDependency(Side.Server)] IRobustRandom globalRng
    )
    {
        #pragma warning disable RA0033
        var freakshow = proto.Index<ESMasqueradePrototype>("Freakshow");
        #pragma warning restore RA0033

        for (var i = 0; i < 100; i++)
        {
            var rngSeed = new RngSeed(globalRng);
            var rng1 = rngSeed.IntoRandomizer();
            var rng2 = rngSeed.IntoRandomizer();

            var masquerade = (MasqueradeRoleSet)freakshow.Masquerade;

            Assert.That(masquerade.TryGetMasks(30, rng1, proto, out var masks1));
            Assert.That(masquerade.TryGetMasks(30, rng2, proto, out var masks2));

            Assert.That(masks1!, Is.EquivalentTo(masks2!));
        }
    }

    public sealed class MasqueradeTestData : GameTestData
    {
        // We need to get to run a mode.
        public override PoolSettings PoolSettings { get; } = new()
        {
            Dirty = true,
            DummyTicker = false,
            Connected = true, // Have one real client connected just to catch oddities.
            InLobby = true,
            Destructive = true, // fuck it. We set the preset which is destructive.
        };

        [SidedDependency(Side.Server)] public readonly IPrototypeManager Proto = default!;

        [System(Side.Server)] public readonly GameTicker SGameticker = default!;

        [System(Side.Server)] public readonly ESMasqueradeSystem MasqueradeSys = default!;
    }

    [GameTest<MasqueradeTestData>("Random")]
    [GameTest<MasqueradeTestData>("Freakshow")]
    [GameTest<MasqueradeTestData>("Showdown")]
    public async Task TestMasqueradeStart(MasqueradeTestData data, string protoStr)
    {
        var proto = data.Proto.Index<ESMasqueradePrototype>(protoStr);
        // A smattering of people. Not including the real client.
        var userCount = (proto.Masquerade.MaxPlayers) ?? 35;
        await data.Server.AddDummySessions(userCount - 1);

        await data.Pair.ReallyBeIdle();

        await data.Server.WaitAssertion(() =>
        {
            // Ready everyone up.
            data.SGameticker.ToggleReadyAll(true);

            // Force a masquerade.

            data.SGameticker.SetGamePreset("ESMasqueradeManaged");
            data.MasqueradeSys.ForceMasquerade(proto);

            // Start the round.
            data.SGameticker.StartRound();
        });

        await data.SyncTicks(10);

        // Game should have started
        Assert.That(data.SGameticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
        Assert.That(data.SGameticker.PlayerGameStatuses.Values.All(x => x == PlayerGameStatus.JoinedGame));

        await data.Server.WaitAssertion(() =>
        {
            // Get the game rule, ensure it's running, ensure we don't have any leftover masks.
            Assert.That(data.SQuerySingle(out Entity<ESMasqueradeRuleComponent>? rule),
                "Masquerade didn't start correctly, no rule was found.");

            Assert.That(rule?.Comp.Masquerade,
                Is.Not.Null,
                "By the time the round starts, the masquerade should exist.");

            Assert.That(data.SQueryCount<ESMaskRoleComponent>(),
                Is.EqualTo(userCount),
                "Expected in-game players with everyone assigned masks.");

            // TODO: This should be applicable to random masquerade too instead of being special cased.
            if (rule.Value.Comp.Masquerade!.Masquerade is MasqueradeRoleSet set)
            {
                var roles =
                    data.SQueryList<ESMaskRoleComponent>()
                        .Select(x => x.Comp.Mask!.Value.Id)
                        .OrderDescending();

                Assert.That(set.TryGetMasks(userCount, rule.Value.Comp.Seed.IntoRandomizer(), data.Proto, out var expectedRoles));

                // We don't care about order so we sort both.
                Assert.That(
                    expectedRoles!.Select(x => x.Id).OrderDescending(),
                    Is.EquivalentTo(roles),
                    "The roles in the game did not match what was expected. Either there's nondeterminism, or masks are not being selected properly."
                    );
            }

            data.SGameticker.RestartRound();
        });

        // Clear out our crowd.
        await data.Server.RemoveAllDummySessions();
    }
}
