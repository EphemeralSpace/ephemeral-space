using Content.Shared._ES.Masks.Traitor.Components;
using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

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
        SubscribeLocalEvent<ESCeilingCacheComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<ESCeilingCacheComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<ESCeilingCacheComponent, ESRevealCacheDoAfterEvent>(OnRevealCacheDoAfter);

        SubscribeLocalEvent<ESCeilingCacheContactingComponent, ESRevealCacheAlertEvent>(OnRevealCacheAlert);
    }

    private void OnStartCollide(Entity<ESCeilingCacheComponent> ent, ref StartCollideEvent args)
    {
        if (!_mind.TryGetMind(args.OtherEntity, out var mindUid, out _) ||
            mindUid != ent.Comp.MindId)
            return;
        _alerts.ShowAlert(args.OtherEntity, ent.Comp.CacheAlertProto);
        var comp = EnsureComp<ESCeilingCacheContactingComponent>(args.OtherEntity);
        comp.Cache = ent;
    }

    private void OnEndCollide(Entity<ESCeilingCacheComponent> ent, ref EndCollideEvent args)
    {
        if (!_mind.TryGetMind(args.OtherEntity, out var mindUid, out _) ||
            mindUid != ent.Comp.MindId)
            return;
        _alerts.ClearAlert(args.OtherEntity, ent.Comp.CacheAlertProto);
        RemComp<ESCeilingCacheContactingComponent>(args.OtherEntity);
    }

    private void OnRevealCacheAlert(Entity<ESCeilingCacheContactingComponent> ent, ref ESRevealCacheAlertEvent args)
    {
        if (ent.Comp.DoAfterKey is not null)
            return;

        if (TerminatingOrDeleted(ent.Comp.Cache))
        {
            RemCompDeferred(ent, ent.Comp);
            return;
        }

        var ev = new ESRevealCacheDoAfterEvent();
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            ent.Owner,
            TimeSpan.FromSeconds(3),
            ev,
            ent.Comp.Cache, // TODO: maybe target something else?
            ent.Comp.Cache,
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

        var pos = Transform(ent).Coordinates;
        var cache = PredictedSpawnAtPosition(ent.Comp.CacheLoot, pos);
        PredictedQueueDel(ent);
        _popup.PopupPredicted(Loc.GetString("es-ceiling-cache-popup"), cache, args.User);
        _audio.PlayPredicted(ent.Comp.RevealSound, pos, args.User);
    }
}

[Serializable, NetSerializable]
public sealed partial class ESRevealCacheDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}

