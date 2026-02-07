using Content.IntegrationTests.Tests._Citadel;
using Content.IntegrationTests.Tests._Citadel.Constraints;
using Content.Server._ES.Masks;
using Content.Server.Mind;
using Content.Shared._ES.Masks;
using Content.Shared._ES.Masks.Components;
using Content.Shared.Mind;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._ES.Masks;

[TestFixture]
public sealed class MaskTests : GameTest
{
    [System(Side.Server)] private readonly ESMaskSystem _sMask = default!;
    [System(Side.Server)] private readonly MindSystem _sMind = default!;

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
        await Pair.CreateTestMap();

        _ = await AssignPlayerBody(Player!, playerPrototype: "MobHuman");

        await Server.WaitAssertion(() =>
        {
            _sMind.TryGetMind(Pair.Player!, out var mindEnt, out var mindComp);

            Assert.That(mindEnt, Is.Not.Deleted(Server));

            Entity<MindComponent> mind = (mindEnt, mindComp);

            _sMask.ApplyMask(mind, maskProto);

            _sMask.TryGetMask(mind, out var mask);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(mask, Is.EqualTo(maskProto));

                // Verify a side effect: the mask role entity exists.
                Assert.That(SQueryCount<ESMaskRoleComponent>(), Is.EqualTo(1));
            }
        });
    }
}
