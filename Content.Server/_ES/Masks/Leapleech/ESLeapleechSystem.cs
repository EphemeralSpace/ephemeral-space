using Content.Server._ES.Masks.Leapleech.Components;
using Content.Server._ES.Masks.Objectives.Relays;
using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared.Damage;
using Content.Shared.Gibbing;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Server.Audio;
using Robust.Shared.Utility;

namespace Content.Server._ES.Masks.Leapleech;

public sealed partial class ESLeapleechSystem : EntitySystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private ESEntityTimerSystem _entityTimer = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private ESMaskSystem _mask = default!;
    [Dependency] private MindSystem _mind = default!;
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
        if (!TryComp<MindComponent>(ent, out var mind) ||
            mind.OwnedEntity is not { } owned)
            return;

        EnsureComp<ESDamageTakerRelayComponent>(owned);
    }

    private void OnMindGotAdded(Entity<ESLeapleechComponent> ent, ref MindGotAddedEvent args)
    {
        if (args.TransferEntity is { } owned)
            EnsureComp<ESDamageTakerRelayComponent>(owned);
    }

    private void OnDamageTaken(Entity<ESLeapleechComponent> ent, ref ESDamageTakenEvent args)
    {

        if (args.Origin is not { } origin ||
            _mask.GetTroupeOrNull(origin) == ent.Comp.IgnoreTroupe)
            return;

        if (!_mind.TryGetMind(origin, out _))
            return;

        var damage = DamageSpecifier.GetPositive(args.DamageDone).GetTotal();
        var oldDamage = ent.Comp.LeechedEntities.GetOrNew(origin);
        ent.Comp.LeechedEntities[origin] = oldDamage + damage;
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
            _throwingSystem.TryThrow(projectile, angle.ToVec(), 5f);
        }

        _gibbing.Gib(owned);
    }
}
