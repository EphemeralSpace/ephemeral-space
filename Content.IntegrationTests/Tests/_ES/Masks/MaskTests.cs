using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Server._ES.SecretIdentity;
using Content.Server.Chat;
using Content.Server.Mind;
using Content.Shared._ES.SecretIdentity;
using Content.Shared._ES.SecretIdentity.Components;
using Content.Shared.Guidebook;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._ES.SecretIdentity;

[TestFixture]
[TestMap(TestMapMode.Arena)]
public sealed class MaskTests : GameTest
{
    [SidedDependency(Side.Server)] private readonly SuicideSystem _suicideSystem = default!;

    public override PoolSettings PoolSettings { get; } = new()
    {
        Dirty = true,
        Connected = true, // We need a guy to mask up.
    };

    public static readonly string[] Masks = GameDataScrounger.PrototypesOfKind<ESSecretIdentityPrototype>();
    public static readonly string[] Troupes = GameDataScrounger.PrototypesOfKind<ESTroupePrototype>();

    [Test]
    [TestCaseSource(nameof(Masks))]
    [Description("Assigns each mask alone with no other players.")]
    public async Task AssignMaskAlone(string maskProto)
    {
        var player = await TestPlayer.CreatePlayer(this);

        await Server.WaitAssertion(() =>
        {
            player.SSetMask(maskProto);

            var mask = player.SGetMask();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(mask, Is.EqualTo(maskProto));

                // Verify a side effect: the mask role entity exists.
                Assert.That(SQueryCount<ESSecretIdentityRoleComponent>(), Is.EqualTo(1));
            }
        });
    }

    // Very strong, suitable for extreme violence.
    private static readonly EntProtoId Weapon = "MeleeDebug200";

    private static readonly Dictionary<ProtoId<ESSecretIdentityPrototype>, string> CannotBeAttackerMasks =
        new()
        {
            {"Host", "Has blocked hands and cannot actually pick anything up as result"},
        };

    private static IEnumerable<TestCaseData> AttackerMasks =>
        GameDataScrounger.PrototypesOfKind<ESSecretIdentityPrototype>()
        .WithIgnores(CannotBeAttackerMasks);

    [Test]
    [TestCaseSource(nameof(AttackerMasks))]
    [Description("Has the given mask beat up a crew member, asserting it doesn't fail.")]
    public async Task BeatUpCrewmember(string maskProto)
    {
        var deviant = await TestPlayer.CreatePlayer(this);

        var targetSession = await Server.AddDummySession();

        var target = await AssignPlayerBody(targetSession);

        await Server.WaitPost(() => { deviant.SSetMask(maskProto); });

        // Grant them the Power.
        await deviant.SpawnAndPickUp(Weapon);

        // Be violent. Really violent.
        for (var i = 0; i < 5; i++)
        {
            if (!SDeleted(deviant.SEntity) && !SDeleted(target))
                await deviant.Punch(target, waitOutCooldown: true);
        }

        if (!SDeleted(target))
            await Server.WaitPost(() => _suicideSystem.Suicide(target)); // free them.

        // Few seconds for stuff to settle.
        // Don't worry tests don't run in realtime.
        await RunSeconds(20);
    }

    [Test]
    [TestCaseSource(nameof(Masks))]
    [Description("Has the a crew member beat up the given mask, asserting it doesn't fail.")]
    public async Task GetBeatenUp(string maskProto)
    {
        var deviant = await TestPlayer.CreatePlayer(this);

        var targetSession = await Server.AddDummySession();

        var target = await AssignPlayerBody(targetSession);

        await Server.WaitPost(() =>
        {
            var mind = Server.System<MindSystem>().GetMind(target)!;

            Server.System<ESSecretIdentitySystem>()
                .ApplyMask((mind!.Value, SComp<MindComponent>(mind!.Value)), maskProto);
        });

        // Grant them the Power.
        await deviant.SpawnAndPickUp(Weapon);

        // Be violent. Really violent.
        for (var i = 0; i < 5; i++)
        {
            if (!SDeleted(deviant.SEntity) && !SDeleted(target))
                await deviant.Punch(target, waitOutCooldown: true);
        }

        if (!SDeleted(target))
            await Server.WaitPost(() => _suicideSystem.Suicide(target)); // free them.

        // Few seconds for stuff to settle.
        // Don't worry tests don't run in realtime.
        await RunSeconds(20);
    }

    [Test]
    [TestCaseSource(nameof(Masks))]
    [Description("Ensures every mask has a corresponding guide entry with the same ID.")]
    public async Task EnsureMaskGuideEntries(string maskProto)
    {
        await Server.WaitAssertion(() =>
        {
            Assert.That(Server.ProtoMan.HasIndex<GuideEntryPrototype>(maskProto), $"{maskProto} must have a guide entry with the same ID as the mask!");
        });
    }

    [Test]
    [TestCaseSource(nameof(Troupes))]
    [Description("Ensures every troupe has a corresponding guide entry with the same ID.")]
    public async Task EnsureTroupeGuideEntries(string troupeProto)
    {
        await Server.WaitAssertion(() =>
        {
            Assert.That(Server.ProtoMan.HasIndex<GuideEntryPrototype>(troupeProto), $"{troupeProto} must have a guide entry with the same ID as the troupe!");
        });
    }
}
