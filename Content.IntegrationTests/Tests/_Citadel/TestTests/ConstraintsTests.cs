#nullable enable
using Content.IntegrationTests.Tests._Citadel.Constraints;
using Content.IntegrationTests.Tests._Citadel.Operators;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Citadel.TestTests;

public sealed class ConstraintsTests : GameTest
{
    [Test]
    [TestOf(typeof(CompExistsConstraint))]
    [RunOnSide(Side.Server)]
    public void CompPositive()
    {
        var ent = SSpawn(null);

        Assert.That(ent, Has.Comp<MetaDataComponent>(Server));
    }

    [Test]
    [TestOf(typeof(CompOperator))]
    [RunOnSide(Side.Server)]
    public void CompPropertyAccess()
    {
        var ent = SSpawn(null);

        Assert.That(ent,
            Has
                .Comp<MetaDataComponent>(Server)
                .Property(nameof(MetaDataComponent.EntityDeleted))
                .EqualTo(false)
        );
    }

    [Test]
    [TestOf(typeof(CompExistsConstraint))]
    [RunOnSide(Side.Server)]
    public void CompNegative()
    {
        var ent = SSpawn(null);

        // Arbitrary pick.
        Assert.That(ent, Has.No.Comp<EyeComponent>(Server));
    }

    [Test]
    [TestOf(typeof(LifeStageConstraint))]
    [RunOnSide(Side.Server)]
    public void DeletedPositive()
    {
        var ent = SSpawn(null);

        SDeleteNow(ent);

        Assert.That(ent, Is.Deleted(Server));
    }

    [Test]
    [TestOf(typeof(LifeStageConstraint))]
    [RunOnSide(Side.Server)]
    [Description("Entities that never existed are currently considered deleted.")]
    public void DeletedNeverExisted()
    {
        // We'll never spawn this many ents in tests without it taking all damn day.
        var ent = new EntityUid(int.MaxValue / 2);

        Assert.That(ent, Is.Deleted(Server), "Entites that never existed still count as deleted.");
    }

    [Test]
    [TestOf(typeof(LifeStageConstraint))]
    [RunOnSide(Side.Server)]
    public void DeletedNegative()
    {
        var ent = SSpawn(null);

        Assert.That(ent, Is.Not.Deleted(Server));
    }
}
