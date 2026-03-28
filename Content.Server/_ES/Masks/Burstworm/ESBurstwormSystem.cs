using System.Numerics;
using Content.Server._ES.Masks.Burstworm.Components;
using Content.Server.Popups;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Robust.Server.Audio;

namespace Content.Server._ES.Masks.Burstworm;

public sealed class ESBurstwormSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ESEntityTimerSystem _entityTimer = default!;
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESBurstwormComponent, ESPlayerKilledEvent>(OnPlayerKilled);
        SubscribeLocalEvent<ESBurstwormComponent, ESBurstwormBurstTimerEvent>(OnBurstTimer);
    }

    private void OnPlayerKilled(Entity<ESBurstwormComponent> ent, ref ESPlayerKilledEvent args)
    {
        if (!args.ValidKill)
            return;

        _popup.PopupEntity(
            Loc.GetString("es-parasite-burstworm-warning", ("name", Identity.Entity(args.Killed, EntityManager))),
            args.Killed,
            PopupType.LargeCaution);

        _entityTimer.SpawnTimer(ent, ent.Comp.BurstDelay, new ESBurstwormBurstTimerEvent());
    }

    private void OnBurstTimer(Entity<ESBurstwormComponent> ent, ref ESBurstwormBurstTimerEvent args)
    {
        Burst(ent);
    }

    private void Burst(Entity<ESBurstwormComponent> ent)
    {
        if (!TryComp<MindComponent>(ent, out var mind) ||
            mind.OwnedEntity is not { } owned)
            return;

        _audio.PlayPvs(ent.Comp.BurstSound, owned);

        var angleSegment = MathF.Tau / ent.Comp.ProjectileCount;
        var angle = Angle.Zero;

        for (var i = 0; i < ent.Comp.ProjectileCount; ++i)
        {
            angle += angleSegment;

            var projectile = SpawnNextToOrDrop(ent.Comp.Projectile, owned);
            _gun.ShootProjectile(projectile, angle.ToVec(), Vector2.Zero, owned, owned);
        }
    }
}
