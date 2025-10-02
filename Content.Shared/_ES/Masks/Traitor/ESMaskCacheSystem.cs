using Content.Shared._ES.Auditions.Components;
using Content.Shared._ES.Masks.Traitor.Components;
using Content.Shared._ES.SpawnRegion;
using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Content.Shared.Mind;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Masks.Traitor;

public sealed class ESMaskCacheSystem : EntitySystem
{
    [Dependency] private readonly ESSharedSpawnRegionSystem _spawnRegion = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly EntProtoId<ESCeilingCacheComponent> CeilingCachePrototype = "ESMarkerTraitorCeilingCache";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESMaskCacheSpawnerComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<ESCeilingCacheComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<ESCeilingCacheComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<ESCeilingCacheComponent, ESRevealCacheDoAfterEvent>(OnRevealCacheDoAfter);

        SubscribeLocalEvent<ESCeilingCacheContactingComponent, ESRevealCacheAlertEvent>(OnRevealCacheAlert);
    }

    private void OnMapInit(Entity<ESMaskCacheSpawnerComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<ESCharacterComponent>(ent, out var character))
            return;

        if (!_spawnRegion.TryGetRandomAreaCoords(ent.Comp.Region, character.Station, out var coords, CollisionGroup.MachineLayer))
        {
            Log.Debug("Failed to find spawn region!");
            return;
        }

        var spawner = PredictedSpawnAtPosition(CeilingCachePrototype, coords.Value);
        var comp = EnsureComp<ESCeilingCacheComponent>(spawner);
        comp.MindId = ent;
        comp.CacheLoot = ent.Comp.CacheProto;
        Dirty(spawner, comp);
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

        var pos = _transform.GetMapCoordinates(ent);
        var cache = PredictedSpawnAtPosition(ent.Comp.CacheLoot, _transform.ToCoordinates(pos));
        PredictedQueueDel(ent);
        _popup.PopupPredicted(Loc.GetString("es-ceiling-cache-popup"), cache, args.User);
        // SFX
    }
}

[Serializable, NetSerializable]
public sealed partial class ESRevealCacheDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}

