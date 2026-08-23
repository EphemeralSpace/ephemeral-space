using System.Diagnostics.CodeAnalysis;
using Content.Server.Pinpointer;
using Content.Shared._ES.Auditions.Components;
using Content.Shared._ES.SecretIdentity.Traitor;
using Content.Shared._ES.SecretIdentity.Traitor.Components;
using Content.Shared._ES.SpawnRegion;
using Content.Shared.EntityTable;
using Content.Shared.Localizations;
using Content.Shared.Mind;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._ES.SecretIdentity.Traitor;

public sealed partial class ESSecretIdentityCacheSystem : ESSharedSecretIdentityCacheSystem
{
    [Dependency] private EntityTableSystem _entityTable = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private ESSecretIdentitySystem _secretIdentity = default!;
    [Dependency] private ESSharedSpawnRegionSystem _spawnRegion = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindComponent, ESAddCacheSecretIdentityModifierEvent>(OnAddCacheModifier);
        SubscribeLocalEvent<ESSecretIdentityCacheSpawnerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnAddCacheModifier(Entity<MindComponent> ent, ref ESAddCacheSecretIdentityModifierEvent args)
    {
        if (!TryComp<ESCharacterComponent>(ent, out var character))
            return;

        var comp = EnsureComp<ESSecretIdentityCacheSpawnerComponent>(ent);

        foreach (var cache in _entityTable.GetSpawns(args.CacheProto))
        {
            TrySpawnCache((ent, comp), cache, args.Region, character.Station, out _);
        }
        _secretIdentity.RefreshCharacterInfoBlurb(ent.AsNullable());
    }

    private void OnMapInit(Entity<ESSecretIdentityCacheSpawnerComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<ESCharacterComponent>(ent, out var character))
            return;

        foreach (var cache in _entityTable.GetSpawns(ent.Comp.CacheProto))
        {
            TrySpawnCache(ent, cache, ent.Comp.Region, character.Station, out _);
        }

        _secretIdentity.RefreshCharacterInfoBlurb(ent.Owner);
    }

    public bool TrySpawnCache(
        Entity<ESSecretIdentityCacheSpawnerComponent> ent,
        EntProtoId cache,
        ProtoId<ESSpawnRegionPrototype> region,
        EntityUid station,
        [NotNullWhen(true)] out EntityCoordinates? coords)
    {
        if (!_spawnRegion.TryGetRandomCoordsInRegion(
                region,
                station,
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

        // Update Briefing
        var mapCoord = TransformSystem.ToMapCoordinates(coords.Value);
        var (x, y) = (Vector2i) mapCoord.Position.Rounded();
        var loc = FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString(mapCoord));

        ent.Comp.Locations.Add(Loc.GetString("es-ceiling-cache-location-format", ("location", loc), ("x", x), ("y", y)));

        ent.Comp.LocationString = Loc.GetString("es-ceiling-cache-location-briefing",
            ("locations", ContentLocalizationManager.FormatList(ent.Comp.Locations)),
            ("count", ent.Comp.Locations.Count));
        Dirty(ent);

        return true;
    }
}
