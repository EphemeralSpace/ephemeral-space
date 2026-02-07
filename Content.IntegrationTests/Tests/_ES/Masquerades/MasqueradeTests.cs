#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Tests._Citadel;
using Content.Server._ES.Masks.Masquerades;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
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
public sealed class MasqueradeTests : GameTest
{
    [SidedDependency(Side.Server)] private readonly ISerializationManager _ser = default!;
    [SidedDependency(Side.Server)] private readonly IPrototypeManager _proto = default!;
    [SidedDependency(Side.Server)] private readonly IRobustRandom _globalRng = default!;

    [Test]
    [RunOnSide(Side.Server)]
    public void TestMasqueradeSerialization()
    {
        // We don't support this yet, so whichever shmuck adds it is gonna need to write tests.
        // My advice is to do a round-trip test where you deserialize entries, then reserialize them and verify they're string equal.
        Assert.Throws<NotImplementedException>(() =>
        {
            _ser.WriteValue<MasqueradeEntry>(
                new MasqueradeEntry.DirectEntry(new HashSet<ProtoId<ESMaskPrototype>>() { "Foo", "Bar" }, 1, false),
                notNullableOverride: true);
        });
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void MasqueradeEntryDeterminism()
    {
        void TestOnEntry(MasqueradeEntry entry)
        {
            // This can in theory flake so let's try a few times.
            for (var i = 0; i < 100; i++)
            {
                var rngSeed = new RngSeed(_globalRng);
                var rng1 = rngSeed.IntoRandomizer();
                var rng2 = rngSeed.IntoRandomizer();

                var masks1 = entry!.PickMasks(rng1, _proto);
                var masks2 = entry!.PickMasks(rng2, _proto);

                Assert.That(masks1,
                    Is.EqualTo(masks2),
                    $"Expected two calls from the same seed to be identical, and they weren't. Seed is {rngSeed.ToString()}");
            }
        }

        {
            MasqueradeEntry.TryRead("Foo/Bar/Baz", null, out var entry, out var error);

            Assert.That(error, Is.Null);

            TestOnEntry(entry!);
        }

        {
            MasqueradeEntry.TryRead("#Freaks", null, out var entry, out var error);

            Assert.That(error, Is.Null);

            TestOnEntry(entry!);
        }
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void MasqueradeDeterminism()
    {
#pragma warning disable RA0033
        var traitors = _proto.Index<ESMasqueradePrototype>("Traitors");
#pragma warning restore RA0033

        for (var i = 0; i < 100; i++)
        {
            var rngSeed = new RngSeed(_globalRng);
            var rng1 = rngSeed.IntoRandomizer();
            var rng2 = rngSeed.IntoRandomizer();

            var masquerade = (MasqueradeRoleSet)traitors.Masquerade;

            Assert.That(masquerade.TryGetMasks(30, rng1, _proto, out var masks1));
            Assert.That(masquerade.TryGetMasks(30, rng2, _proto, out var masks2));

            Assert.That(masks1!, Is.EquivalentTo(masks2!));
        }
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void MaskSetsHaveMasks()
    {
        foreach (var maskSet in _proto.EnumeratePrototypes<ESMaskSetPrototype>())
        {
            Assert.That(maskSet.AllMasks(), Is.Not.Empty);
        }
    }
}

public sealed class MasqueradeRunTests : GameTest
{
    [SidedDependency(Side.Server)] private readonly IPrototypeManager _proto = default!;

    [System(Side.Server)] private readonly GameTicker _sGameticker = default!;
    [System(Side.Server)] private readonly ESMasqueradeSystem _sMasqueradeSys = default!;

    public override PoolSettings PoolSettings { get; } = new()
    {
        Dirty = true,
        DummyTicker = false,
        Connected = true, // Have one real client connected just to catch oddities.
        InLobby = true,
    };

    public static readonly string[] Masquerades = PrototypeDataScrounger.PrototypesOfKind<ESMasqueradePrototype>();

    [Test]
    public async Task TestMasqueradeStart(
            [ValueSource(nameof(Masquerades))] string protoStr,
            [Values([35, 21])] int userCount
        )
    {
        var proto = _proto.Index<ESMasqueradePrototype>(protoStr);
        // A smattering of people. Not including the real client.
        await AddDummySessionsSync(userCount - 1);

        await Server.WaitAssertion(() =>
        {
            // Ready everyone up.
            _sGameticker.ToggleReadyAll(true);

            // Force a masquerade.
            _sGameticker.SetGamePreset("ESMasqueradeManaged");
            _sMasqueradeSys.ForceMasquerade(proto);

            // Start the round.
            _sGameticker.StartRound();
        });

        await SyncTicks(10);

        // Game should have started
        Assert.That(_sGameticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
        Assert.That(_sGameticker.PlayerGameStatuses.Values.All(x => x == PlayerGameStatus.JoinedGame));

        await Server.WaitAssertion(() =>
        {
            // Get the game rule, ensure it's running, ensure we don't have any leftover masks.
            Assert.That(SQuerySingle(out Entity<ESMasqueradeRuleComponent>? rule),
                "Masquerade didn't start correctly, no rule was found.");

            Assert.That(rule?.Comp.Masquerade,
                Is.Not.Null,
                "By the time the round starts, the masquerade should exist.");

            Assert.That(SQueryCount<ESMaskRoleComponent>(),
                Is.EqualTo(userCount),
                "Expected in-game players with everyone assigned masks.");

            // TODO: This should be applicable to random masquerade too instead of being special cased.
            if (rule.Value.Comp.Masquerade!.Masquerade is MasqueradeRoleSet set)
            {
                var roles =
                    SQueryList<ESMaskRoleComponent>()
                        .Select(x => x.Comp.Mask!.Value.Id)
                        .OrderDescending();

                Assert.That(set.TryGetMasks(userCount, rule.Value.Comp.Seed.IntoRandomizer(), _proto, out var expectedRoles));

                // We don't care about order so we sort both.
                Assert.That(
                    expectedRoles!.Select(x => x.Id).OrderDescending(),
                    Is.EquivalentTo(roles),
                    "The roles in the game did not match what was expected. Either there's nondeterminism, or masks are not being selected properly."
                    );
            }

            _sGameticker.RestartRound();

            _sGameticker.SetGamePreset((GamePresetPrototype?) null);
        });
    }
}
