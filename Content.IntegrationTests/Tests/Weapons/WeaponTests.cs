using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Damage.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Weapons;

public sealed class WeaponTests : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman"; // The default test mob only has one hand
    private static readonly EntProtoId MobHuman = "MobHuman";
    private static readonly EntProtoId TestGun = "TestWeaponGunWieldable";

    [Test]
    public async Task GunRequiresWieldTest()
    {
        var gunSystem = SEntMan.System<SharedGunSystem>();

        await AddAtmosphere(); // prevent the Urist from suffocating

        var urist = await SpawnTarget(MobHuman);
        var damageComp = Comp<DamageableComponent>(urist);

        var gunNet = await PlaceInHands(TestGun);
        var gunEnt = ToServer(gunNet);

        await Pair.RunSeconds(2f); // Guns have a cooldown when picking them up.

        Assert.That(HasComp<GunRequiresWieldComponent>(gunNet),
            "Looks like you've removed the 'GunRequiresWield' component from the test gun." +
            "what are you doing bro its in the damn name!");

        var startAmmo = gunSystem.GetAmmoCount(gunEnt);
        var wieldComp = Comp<WieldableComponent>(gunNet);

        Assert.That(startAmmo, Is.GreaterThan(0), "Test gun was spawned with no ammo!");
        Assert.That(wieldComp.Wielded, Is.False, "Test gun was spawned wielded!");

        await AttemptShoot(urist, false); // should fail due to not being wielded
        var updatedAmmo = gunSystem.GetAmmoCount(gunEnt);

        Assert.That(updatedAmmo,
            Is.EqualTo(startAmmo),
            "Test gun discharged ammo when the weapon should not have fired!");
        Assert.That(damageComp.TotalDamage.Value,
            Is.EqualTo(0),
            "Urist took damage when the weapon should not have fired!");

        await UseInHand();

        Assert.That(wieldComp.Wielded, Is.True, "Test gun failed to wield when interacted with!");

        await AttemptShoot(urist);
        updatedAmmo = gunSystem.GetAmmoCount(gunEnt);

        Assert.That(updatedAmmo, Is.EqualTo(startAmmo - 1), "Test gun failed to discharge appropriate amount of ammo!");
        Assert.That(damageComp.TotalDamage.Value,
            Is.GreaterThan(0),
            "Test gun was fired but urist sustained no damage!");
    }
}
