using Content.IntegrationTests.Tests._Citadel;
using Content.IntegrationTests.Tests._Citadel.Attributes;
using Content.Server._ES.Masks;
using Content.Server.Chat;
using Content.Server.Mind;
using Content.Shared._ES.Masks;
using Content.Shared._ES.Masks.Components;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._ES.Masks;

[TestFixture]
[TestMap(TestMapMode.Arena)]
public sealed class MaskTests : GameTest
{
    [System(Side.Server)] private readonly SuicideSystem _suicideSystem = default!;

    public override PoolSettings PoolSettings { get; } = new()
    {
        Dirty = true,
        Connected = true, // We need a guy to mask up.
    };

    public static readonly string[] Masks = PrototypeDataScrounger.PrototypesOfKind<ESMaskPrototype>();

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
                Assert.That(SQueryCount<ESMaskRoleComponent>(), Is.EqualTo(1));
            }
        });
    }

    // Very strong, suitable for extreme violence.
    private static readonly EntProtoId Weapon = "MeleeDebug200";

    [Test]
    [TestCaseSource(nameof(Masks))]
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

            Server.System<ESMaskSystem>()
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
}
