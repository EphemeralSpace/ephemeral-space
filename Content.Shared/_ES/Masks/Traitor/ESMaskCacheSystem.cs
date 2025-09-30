using Content.Shared._ES.Auditions.Components;
using Content.Shared._ES.Masks.Traitor.Components;
using Content.Shared._ES.SpawnRegion;
using Content.Shared.Alert;
using Content.Shared.Mind;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks.Traitor;

public sealed class ESMaskCacheSystem : EntitySystem
{
    [Dependency] private readonly ESSharedSpawnRegionSystem _spawnRegion = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    private static readonly EntProtoId<ESCeilingCacheComponent> CeilingCachePrototype = "ESMarkerTraitorCeilingCache";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESMaskCacheSpawnerComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<ESCeilingCacheComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<ESCeilingCacheComponent, EndCollideEvent>(OnEndCollide);
    }

    private void OnMapInit(Entity<ESMaskCacheSpawnerComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<ESCharacterComponent>(ent, out var character))
            return;

        if (!_spawnRegion.TryGetRandomAreaCoords(ent.Comp.Region, character.Station, out var coords))
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
    }

    private void OnEndCollide(Entity<ESCeilingCacheComponent> ent, ref EndCollideEvent args)
    {
        if (!_mind.TryGetMind(args.OtherEntity, out var mindUid, out _) ||
            mindUid != ent.Comp.MindId)
            return;
        _alerts.ClearAlert(args.OtherEntity, ent.Comp.CacheAlertProto);
    }
}
