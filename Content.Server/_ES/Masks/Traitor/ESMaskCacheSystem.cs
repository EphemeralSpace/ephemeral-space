using System.Diagnostics.CodeAnalysis;
using Content.Server.Pinpointer;
using Content.Shared._ES.Auditions.Components;
using Content.Shared._ES.Masks.Traitor;
using Content.Shared._ES.Masks.Traitor.Components;
using Content.Shared._ES.SpawnRegion;
using Content.Shared.EntityTable;
using Content.Shared.Localizations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._ES.Masks.Traitor;

public sealed class ESMaskCacheSystem : ESSharedMaskCacheSystem
{
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly ESSharedSpawnRegionSystem _spawnRegion = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESMaskCacheSpawnerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ESMaskCacheSpawnerComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<ESCharacterComponent>(ent, out var character))
            return;

        var coords = new List<EntityCoordinates>();
        foreach (var cache in _entityTable.GetSpawns(ent.Comp.CacheProto))
        {
            if (TrySpawnCache(ent, (ent, character), cache, out var c))
                coords.Add(c.Value);
        }

        var locationStrings = new List<string>();
        foreach (var coord in coords)
        {
            var mapCoord = TransformSystem.ToMapCoordinates(coord);
            var (x, y) = (Vector2i) mapCoord.Position.Rounded();
            var loc = FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString(mapCoord));

            locationStrings.Add(Loc.GetString("es-ceiling-cache-location-format", ("location", loc), ("x", x), ("y", y)));
        }

        ent.Comp.LocationString = Loc.GetString("es-ceiling-cache-location-briefing",
            ("locations", ContentLocalizationManager.FormatList(locationStrings)),
            ("count", locationStrings.Count));
        Dirty(ent);
    }

    private bool TrySpawnCache(Entity<ESMaskCacheSpawnerComponent> ent, Entity<ESCharacterComponent> character, EntProtoId cache, [NotNullWhen(true)] out EntityCoordinates? coords)
    {
        if (!_spawnRegion.TryGetRandomCoordsInRegion(
                ent.Comp.Region,
                character.Comp.Station,
                out coords,
                checkPlayerLOS: false,
                minPlayerDistance: 0f))
        {
            Log.Debug("Failed to find spawn region!");
            return false;
        }

        var spawner = SpawnAtPosition(CeilingCachePrototype, coords.Value);
        var comp = EnsureComp<ESCeilingCacheComponent>(spawner);
        comp.MindId = ent;
        comp.CacheLoot = cache;
        Dirty(spawner, comp);

        return true;
    }
}
