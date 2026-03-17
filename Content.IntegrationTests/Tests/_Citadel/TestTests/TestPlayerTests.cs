using System.Numerics;
using Content.IntegrationTests.Tests._Citadel.Attributes;
using Content.IntegrationTests.Tests._Citadel.Constraints;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Citadel.TestTests;

public sealed partial class TestPlayerTests : GameTest
{
    private static readonly string[] TestMobs = ["InteractionTestMob", "MobHuman", "MobObserver", "AdminObserver"];

    [Test]
    [TestCaseSource(nameof(TestMobs))]
    [TestMap(TestMapMode.Basic)]
    public async Task MakePlayer(string proto)
    {
        var player = await TestPlayer.CreatePlayer(this, playerProto: proto);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player, Is.Initialized(Client));
            Assert.That(player, Is.MapInitialized(Server));

            Assert.That(player.SEntity, Is.EqualTo(ServerSession!.AttachedEntity));

            Assert.That(player, Is.MapInitialized(Server));
            Assert.That(player.SMindEntity.Comp.UserId, Is.EqualTo(ServerSession!.UserId));
        }
    }

    [Test]
    public async Task ErroneousNoMap()
    {
        // No map made here.

        Assert.CatchAsync<NotSupportedException>(async () =>
        {
            _ = await TestPlayer.CreatePlayer(this);
        });
    }

    [Test]
    [TestMap(TestMapMode.Basic)]
    public async Task ErroneousMakeManyPlayers()
    {
        _ = await TestPlayer.CreatePlayer(this);

        Assert.CatchAsync<NotSupportedException>(async () =>
        {
            // We didn't detach first...
            _ = await TestPlayer.CreatePlayer(this);
        });
    }

    [Test]
    [TestMap(TestMapMode.Basic)]
    public async Task MakeManyPlayers()
    {
        var player = await TestPlayer.CreatePlayer(this);

        await player.Destroy();

        _ = await TestPlayer.CreatePlayer(this);
    }

    [Test]
    [TestMap(TestMapMode.Arena)]
    public async Task WalkIntoVoid()
    {
        var player = await TestPlayer.CreatePlayer(this);

        var xform = SComp<TransformComponent>(player.SEntity);

        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(xform.LocalPosition, Is.EqualTo(Vector2.Zero));
                Assert.That(xform.ParentUid, Is.Not.EqualTo(xform.MapUid));
            }
        });

        await player.Move(DirectionFlag.North, 2);

        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(xform.LocalPosition, Is.Not.EqualTo(Vector2.Zero));
                Assert.That(xform.ParentUid, Is.EqualTo(xform.MapUid));
            }
        });
    }

    [Test]
    [TestMap(TestMapMode.Arena)]
    public async Task Punch()
    {
        var player = await TestPlayer.CreatePlayer(this);

        var pos = new Vector2(-1, 0);

        var target = await SpawnAtPosition("MobHuman", TestMap.GridCoords.Offset(pos));

        await RunUntilSynced();

        Assert.That(target,
            Has.Comp<TransformComponent>(Server)
                .With.Property(nameof(TransformComponent.LocalPosition))
                .EqualTo(pos));

        var damage = SComp<DamageableComponent>(target);

        for (var i = 0; i < 3; i++)
        {
            var initialBlunt = damage.Damage.DamageDict["Blunt"];
            // Do a crime.
            await player.Punch(target, waitOutCooldown: true);
            Assert.That(damage.Damage.DamageDict["Blunt"], Is.Not.EqualTo(initialBlunt), $"Punch didn't deal damage? Punch #{i}");
        }
    }

    [Test]
    [Description("Has the player sit in place innocently in the given maps. Ensures they're unharmed.")]
    [TestCase(TestMapMode.Arena)]
    public async Task SitAroundInnocently(TestMapMode mode)
    {
        await CreateTestMap(mode);

        var player = await TestPlayer.CreatePlayer(this);

        await RunSeconds(10);

        var damage = SComp<DamageableComponent>(player.SEntity);

        Assert.That(damage.Damage.DamageDict.Values, Is.All.EqualTo(FixedPoint2.Zero));
    }
}
