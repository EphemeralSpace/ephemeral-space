using System.Numerics;
using Content.Server._ES.Masks.Burstworm.Components;
using Content.Server._ES.Masks.Leapleech.Components;
using Content.Server._ES.Masks.Objectives.Relays;
using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Server.Popups;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Server.Audio;

namespace Content.Server._ES.Masks.Leapleech;

public sealed class ESLeapleechSystem : EntitySystem
{
    public Type[] RelayComponents => new[] { typeof(ESDamageTakerRelayComponent) };

    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private ESEntityTimerSystem _entityTimer = default!;
    [Dependency] private GunSystem _gun = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private ThrowingSystem _throwingSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ESLeapleechComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<ESLeapleechComponent, MindGotAddedEvent>(OnMindGotAdded);

        SubscribeLocalEvent<ESLeapleechComponent, ESDamageTakenEvent>(OnDamageTaken);
        SubscribeLocalEvent<ESLeapleechComponent, ESPlayerKilledEvent>(OnPlayerKilled);
        SubscribeLocalEvent<ESLeapleechComponent, ESLeapLeechBurstTimerEvent>(OnBurstTimer);
    }

    private void OnComponentStartup(Entity<ESLeapleechComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<MindComponent>(ent, out var mind))
            return;

        if (!HasComp<ESDamageTakerRelayComponent>(mind.OwnedEntity))
            AddComp<ESDamageTakerRelayComponent>((EntityUid)mind.OwnedEntity!);
    }
    private void OnMindGotAdded(Entity<ESLeapleechComponent> ent, ref MindGotAddedEvent args)
    {
        if (!HasComp<ESDamageTakerRelayComponent>(args.Mind.Comp.OwnedEntity))
            AddComp<ESDamageTakerRelayComponent>((EntityUid)args.Mind.Comp.OwnedEntity!);
    }

    private void OnDamageTaken(Entity<ESLeapleechComponent> ent, ref ESDamageTakenEvent args)
    {
        if (!TryComp<ESKillTrackerComponent>(args.Body, out var tracker))
            return;

        foreach (var entity in tracker.Sources)
        {
            if (entity.Entity == null)
                return;

            var source = (EntityUid)entity.Entity;

            if (!HasComp<MindContainerComponent>(source) || source == ent.Owner)
                return;

            if (ent.Comp.LeechedEntities.Contains(source) || entity.AccumulatedDamage < 30)
                return;

            ent.Comp.LeechCount += 1;
            ent.Comp.LeechedEntities.Add(source);
        }
    }

    private void OnPlayerKilled(Entity<ESLeapleechComponent> ent, ref ESPlayerKilledEvent args)
    {
        if (!args.ValidKill || ent.Comp.LeechCount == 0)
            return;

        _popup.PopupEntity(
            Loc.GetString("es-parasite-burstworm-warning", ("name", Identity.Entity(args.Killed, EntityManager))),
            args.Killed,
            PopupType.LargeCaution);

        _entityTimer.SpawnTimer(ent, ent.Comp.BurstDelay, new ESLeapLeechBurstTimerEvent());
    }

    private void OnBurstTimer(Entity<ESLeapleechComponent> ent, ref ESLeapLeechBurstTimerEvent args)
    {
        Burst(ent);
    }

    private void Burst(Entity<ESLeapleechComponent> ent)
    {
        if (!TryComp<MindComponent>(ent, out var mind) ||
            mind.OwnedEntity is not { } owned)
            return;

        _audio.PlayPvs(ent.Comp.BurstSound, owned);

        var angleSegment = MathF.Tau / ent.Comp.LeechCount;
        var angle = Angle.Zero;

        for (var i = 0; i < ent.Comp.LeechCount; ++i)
        {
            angle += angleSegment;

            var projectile = SpawnNextToOrDrop(ent.Comp.Projectile, owned);
            _throwingSystem.TryThrow(projectile, angle.ToVec(), 1f);
        }
    }



}
