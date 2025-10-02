using Content.Server.Pinpointer;
using Content.Server.Roles;
using Content.Shared._ES.Auditions.Components;
using Content.Shared._ES.Masks.Traitor;
using Content.Shared._ES.Masks.Traitor.Components;
using Content.Shared._ES.SpawnRegion;
using Content.Shared.Physics;
using Robust.Shared.Utility;

namespace Content.Server._ES.Masks.Traitor;

public sealed class ESMaskCacheSystem : ESSharedMaskCacheSystem
{
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly ESSharedSpawnRegionSystem _spawnRegion = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESMaskCacheSpawnerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ESMaskCacheSpawnerComponent, GetBriefingEvent>(OnGetBriefing);
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

        var spawner = SpawnAtPosition(CeilingCachePrototype, coords.Value);
        var comp = EnsureComp<ESCeilingCacheComponent>(spawner);
        comp.MindId = ent;
        comp.CacheLoot = ent.Comp.CacheProto;

        var mapCoords = TransformSystem.ToMapCoordinates(coords.Value);
        var locationStr = FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString(mapCoords));
        var (x, y) = mapCoords.Position.Rounded();
        ent.Comp.LocationBriefing = Loc.GetString("es-ceiling-cache-location-briefing", ("location", locationStr), ("x", (int) x), ("y", (int) y));

        Dirty(spawner, comp);
    }

    private void OnGetBriefing(Entity<ESMaskCacheSpawnerComponent> ent, ref GetBriefingEvent args)
    {
        args.Append(ent.Comp.LocationBriefing);
    }
}
