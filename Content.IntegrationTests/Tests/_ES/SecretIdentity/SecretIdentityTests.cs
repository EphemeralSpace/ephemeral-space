using System.Collections.Generic;
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
public sealed class SecretIdentityTests : GameTest
{
    [SidedDependency(Side.Server)] private readonly SuicideSystem _suicideSystem = default!;

    public override PoolSettings PoolSettings { get; } = new()
    {
        Dirty = true,
        Connected = true, // We need a guyyyyyyy
    };

    public static readonly string[] SecretIdentities = GameDataScrounger.PrototypesOfKind<ESSecretIdentityPrototype>();
    public static readonly string[] Organizations = GameDataScrounger.PrototypesOfKind<ESOrganizationPrototype>();

    [Test]
    [TestCaseSource(nameof(SecretIdentities))]
    [Description("Assigns each secret identity alone with no other players.")]
    public async Task AssignSecretIdentityAlone(string secretIdentityProto)
    {
        var player = await TestPlayer.CreatePlayer(this);

        await Server.WaitAssertion(() =>
        {
            player.SSetSecretIdentity(secretIdentityProto);

            var secretIdentity = player.SGetSecretIdentity();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(secretIdentity, Is.EqualTo(secretIdentityProto));

                // Verify a side effect: the secret identity role entity exists.
                Assert.That(SQueryCount<ESSecretIdentityRoleComponent>(), Is.EqualTo(1));
            }
        });
    }

    // Very strong, suitable for extreme violence.
    private static readonly EntProtoId Weapon = "MeleeDebug200";

    private static readonly Dictionary<ProtoId<ESSecretIdentityPrototype>, string> CannotBeAttackerIdentities = [];

    private static IEnumerable<TestCaseData> AttackerIdentities =>
        GameDataScrounger.PrototypesOfKind<ESSecretIdentityPrototype>()
        .WithIgnores(CannotBeAttackerIdentities);

    [Test]
    [TestCaseSource(nameof(AttackerIdentities))]
    [Description("Has the given secret identity beat up a crew member, asserting it doesn't fail.")]
    public async Task BeatUpCrewmember(string secretIdentityProto)
    {
        var deviant = await TestPlayer.CreatePlayer(this);

        var targetSession = await Server.AddDummySession();

        var target = await AssignPlayerBody(targetSession);

        await Server.WaitPost(() => { deviant.SSetSecretIdentity(secretIdentityProto); });

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
    [TestCaseSource(nameof(SecretIdentities))]
    [Description("Has the a crew member beat up the given secret identity, asserting it doesn't fail.")]
    public async Task GetBeatenUp(string secretIdentityProto)
    {
        var deviant = await TestPlayer.CreatePlayer(this);

        var targetSession = await Server.AddDummySession();

        var target = await AssignPlayerBody(targetSession);

        await Server.WaitPost(() =>
        {
            var mind = Server.System<MindSystem>().GetMind(target)!;

            Server.System<ESSecretIdentitySystem>()
                .ApplySecretIdentity((mind!.Value, SComp<MindComponent>(mind!.Value)), secretIdentityProto);
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
    [TestCaseSource(nameof(SecretIdentities))]
    [Description("Ensures every mask has a corresponding guide entry with the same ID.")]
    public async Task EnsureSecretIdentityGuideEntries(string secretIdentityProto)
    {
        await Server.WaitAssertion(() =>
        {
            Assert.That(Server.ProtoMan.HasIndex<GuideEntryPrototype>(secretIdentityProto), $"{secretIdentityProto} must have a guide entry with the same ID as the secret identity!");
        });
    }

    [Test]
    [TestCaseSource(nameof(Organizations))]
    [Description("Ensures every organization has a corresponding guide entry with the same ID.")]
    public async Task EnsureOrganizationGuideEntries(string organizationProto)
    {
        await Server.WaitAssertion(() =>
        {
            Assert.That(Server.ProtoMan.HasIndex<GuideEntryPrototype>(organizationProto), $"{organizationProto} must have a guide entry with the same ID as the organization!");
        });
    }
}
