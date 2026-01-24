using Content.Shared._ES.Masks.Traitor.Components;
using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Masks.Traitor;

public abstract class ESSharedMaskCacheSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;

    protected static readonly EntProtoId<ESCeilingCacheComponent> CeilingCachePrototype = "ESMarkerTraitorCeilingCache";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESMaskCacheSpawnerComponent, ESGetCharacterInfoBlurbEvent>(OnGetCharacterInfoBlurb);

        SubscribeLocalEvent<ESCeilingCacheComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<ESCeilingCacheComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<ESCeilingCacheComponent, ESRevealCacheDoAfterEvent>(OnRevealCacheDoAfter);

        SubscribeLocalEvent<ESCeilingCacheContactingComponent, ESRevealCacheAlertEvent>(OnRevealCacheAlert);
    }

    private void OnGetCharacterInfoBlurb(Entity<ESMaskCacheSpawnerComponent> ent, ref ESGetCharacterInfoBlurbEvent args)
    {
        args.Info.Add(FormattedMessage.FromMarkupOrThrow(ent.Comp.LocationString));
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

    public void RevealCache(Entity<ESCeilingCacheComponent?> ent, EntityUid? user)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var pos = Transform(ent).Coordinates;
        var cache = PredictedSpawnAtPosition(ent.Comp.CacheLoot, pos);
        PredictedQueueDel(ent);
        _popup.PopupPredicted(Loc.GetString("es-ceiling-cache-popup"), cache, user);
        _audio.PlayPredicted(ent.Comp.RevealSound, pos, user);

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

