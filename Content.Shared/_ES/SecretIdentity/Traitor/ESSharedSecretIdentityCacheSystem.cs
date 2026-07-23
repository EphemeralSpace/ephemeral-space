using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.SecretIdentity.Traitor.Components;
using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._ES.SecretIdentity.Traitor;

public abstract partial class ESSharedSecretIdentityCacheSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private ESEntityTimerSystem _entityTimer = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] protected SharedTransformSystem TransformSystem = default!;

    protected static readonly EntProtoId<ESCeilingCacheComponent> CeilingCachePrototype = "ESMarkerTraitorCeilingCache";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESSecretIdentityCacheSpawnerComponent, ESGetCharacterInfoBlurbEvent>(OnGetCharacterInfoBlurb);
        SubscribeLocalEvent<ESSecretIdentityCacheSpawnerComponent, MindRelayedEvent<MobStateChangedEvent>>(OnMobStateChanged);

        SubscribeLocalEvent<ESCeilingCacheComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<ESCeilingCacheComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<ESCeilingCacheComponent, ESRevealCacheDoAfterEvent>(OnRevealCacheDoAfter);
        SubscribeLocalEvent<ESCeilingCacheComponent, ESRevealCacheTimerEvent>(OnRevealCacheTimer);

        SubscribeLocalEvent<ESCeilingCacheContactingComponent, ESRevealCacheAlertEvent>(OnRevealCacheAlert);
    }

    private void OnGetCharacterInfoBlurb(Entity<ESSecretIdentityCacheSpawnerComponent> ent, ref ESGetCharacterInfoBlurbEvent args)
    {
        args.Info.Add(FormattedMessage.FromMarkupOrThrow(ent.Comp.LocationString));
    }

    private void OnMobStateChanged(Entity<ESSecretIdentityCacheSpawnerComponent> ent, ref MindRelayedEvent<MobStateChangedEvent> args)
    {
        if (args.Args.NewMobState != MobState.Dead)
            return;

        var query = EntityQueryEnumerator<ESCeilingCacheComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.MindId != ent)
                continue;

            var revealDelay = _random.Next(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(15));
            _entityTimer.SpawnTimer(uid, revealDelay, new ESRevealCacheTimerEvent());
        }
    }

    private void OnStartCollide(Entity<ESCeilingCacheComponent> ent, ref StartCollideEvent args)
    {
        if (!_mind.TryGetMind(args.OtherEntity, out var mindUid, out _) ||
            mindUid != ent.Comp.MindId)
            return;
        _alerts.ShowAlert(args.OtherEntity, ent.Comp.CacheAlertProto);
        var comp = EnsureComp<ESCeilingCacheContactingComponent>(args.OtherEntity);
        comp.Caches.Add(ent);
        Dirty(args.OtherEntity, comp);
    }

    private void OnEndCollide(Entity<ESCeilingCacheComponent> ent, ref EndCollideEvent args)
    {
        if (!TryComp<ESCeilingCacheContactingComponent>(args.OtherEntity, out var cacheComp) ||
            !_mind.TryGetMind(args.OtherEntity, out var mindUid, out _) ||
            mindUid != ent.Comp.MindId)
            return;

        cacheComp.Caches.Remove(ent);
        Dirty(args.OtherEntity, cacheComp);
        if (cacheComp.Caches.Count > 0) // don't remove it if we're touching multiple caches.
            return;

        _alerts.ClearAlert(args.OtherEntity, ent.Comp.CacheAlertProto);
        RemComp(args.OtherEntity, cacheComp);
    }

    private void OnRevealCacheAlert(Entity<ESCeilingCacheContactingComponent> ent, ref ESRevealCacheAlertEvent args)
    {
        if (ent.Comp.Caches.FirstOrNull() is not { } cache)
            return;

        if (TerminatingOrDeleted(cache))
        {
            RemCompDeferred(ent, ent.Comp);
            return;
        }

        var ev = new ESRevealCacheDoAfterEvent();
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            ent.Owner,
            TimeSpan.FromSeconds(3),
            ev,
            cache,
            cache,
            ent.Owner
            )
            {
                BreakOnMove = true,
                BlockDuplicate = true,
                DuplicateCondition = DuplicateConditions.SameTarget,
            });
    }

    private void OnRevealCacheDoAfter(Entity<ESCeilingCacheComponent> ent, ref ESRevealCacheDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        RevealCache(ent.AsNullable(), args.User);
    }

    private void OnRevealCacheTimer(Entity<ESCeilingCacheComponent> ent, ref ESRevealCacheTimerEvent args)
    {
        RevealCache(ent.AsNullable(), null);
    }

    public void RevealCache(Entity<ESCeilingCacheComponent?> ent, EntityUid? user)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var pos = Transform(ent).Coordinates;
        var cache = PredictedSpawnAtPosition(ent.Comp.CacheLoot, pos);
        PredictedQueueDel(ent);
        _popup.PopupEntity(Loc.GetString("es-ceiling-cache-popup"), ent);
        _audio.PlayPredicted(ent.Comp.RevealSound, pos, user, ent.Comp.RevealSound?.Params.WithMaxDistance(1.5f).WithVolume(-3f));

        if (ent.Comp.MindId.HasValue)
        {
            var ev = new ESCacheRevealedEvent(cache);
            RaiseLocalEvent(ent.Comp.MindId.Value, ref ev);
        }
    }
}

[Serializable, NetSerializable]
public sealed partial class ESRevealCacheDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}

[ByRefEvent]
public readonly record struct ESCacheRevealedEvent(EntityUid Cache);

