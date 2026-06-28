using System.Numerics;
using Content.Shared.Stunnable;
using Content.Shared.Damage.Components;
using Content.Shared.Effects;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Damage.Systems;

public sealed partial class DamageOnHighSpeedImpactSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedColorFlashEffectSystem _color = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageOnHighSpeedImpactComponent, StartCollideEvent>(HandleCollide);
    }

    private void HandleCollide(EntityUid uid, DamageOnHighSpeedImpactComponent component, ref StartCollideEvent args)
    {
        if (!args.OurFixture.Hard || !args.OtherFixture.Hard)
            return;

        if (!HasComp<DamageableComponent>(uid))
            return;

        //TODO: This should solve after physics solves
        var speed = args.OurBody.LinearVelocity.Length();

        if (speed < component.MinimumSpeed)
            return;

        if (component.LastHit != null
            && (_gameTiming.CurTime - component.LastHit.Value) < component.DamageCooldown)
            return;

        component.LastHit = _gameTiming.CurTime;

        _stun.TryUpdateParalyzeDuration(uid, component.StunTime);

        var damageScale = component.SpeedDamageFactor * speed / component.MinimumSpeed;

        _damageable.TryChangeDamage(uid, component.Damage * damageScale);

        var msg = Loc.GetString("es-damage-high-speed-impact-impacted",
            ("entity", uid),
            ("impacted", args.OtherEntity));
        _popup.PopupEntity(msg, uid, Filter.Pvs(uid), true, PopupType.MediumCaution);

        _audio.PlayPredicted(component.SoundHit, uid, uid, AudioParams.Default.WithVariation(0.125f).WithVolume(-0.125f));
        _color.RaiseEffect(Color.Red, new List<EntityUid>() { uid }, Filter.Pvs(uid, entityManager: EntityManager));
        _physics.SetLinearVelocity(uid, Vector2.Zero);
    }

    public void ChangeCollide(EntityUid uid, float minimumSpeed, float stunSeconds, float damageCooldown, float speedDamage, DamageOnHighSpeedImpactComponent? collide = null)
    {
        if (!Resolve(uid, ref collide, false))
            return;

        // TODO ew what the fuck
        collide.MinimumSpeed = minimumSpeed;
        collide.StunTime = TimeSpan.FromSeconds(stunSeconds);
        collide.DamageCooldown = TimeSpan.FromSeconds(damageCooldown);
        collide.SpeedDamageFactor = speedDamage;
        Dirty(uid, collide);
    }
}
