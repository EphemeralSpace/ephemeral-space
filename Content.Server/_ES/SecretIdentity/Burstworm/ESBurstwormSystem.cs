using System.Numerics;
using Content.Server._ES.SecretIdentity.Burstworm.Components;
using Content.Server.Popups;
using Content.Server.Storage.EntitySystems;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared.Gibbing;
using Content.Shared.IdentityManagement;
using Content.Shared.Lock;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Robust.Server.Audio;
using Robust.Server.Containers;

namespace Content.Server._ES.SecretIdentity.Burstworm;

public sealed partial class ESBurstwormSystem : EntitySystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private EntityStorageSystem _entityStorage = default!;
    [Dependency] private ESEntityTimerSystem _entityTimer = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private GunSystem _gun = default!;
    [Dependency] private LockSystem _lock = default!;
    [Dependency] private PopupSystem _popup = default!;

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

        if (_container.TryGetContainingContainer(owned, out var container) &&
            TryComp<EntityStorageComponent>(container.Owner, out var storage))
        {
            _lock.Unlock(container.Owner, null);
            _entityStorage.OpenStorage(container.Owner, storage);
        }

        _gibbing.Gib(owned);
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
