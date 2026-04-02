using System.Linq;
using Content.Server._ES.Masks.Traitor.Components;
using Content.Server.Nuke;
using Content.Server.RoundEnd;
using Content.Server.Spawners.Components;
using Content.Shared._ES.Masks.Components;
using Content.Shared.Mind;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._ES.Masks.Traitor;

/// <summary>
/// This handles <see cref="ESTraitorRuleComponent"/>
/// </summary>
public sealed class ESTraitorRuleSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<NukeArmedEvent>(OnNukeArmed);
        SubscribeLocalEvent<NukeExplodedEvent>(OnNukeExploded);
    }

    private void OnNukeArmed(NukeArmedEvent ev)
    {
        var query = EntityQueryEnumerator<ESTraitorRuleComponent, ESTroupeRuleComponent>();
        while (query.MoveNext(out var uid, out var traitor, out var troupe))
        {
            OnNukeArmed((uid, traitor, troupe));
        }
    }

    private void OnNukeArmed(Entity<ESTraitorRuleComponent, ESTroupeRuleComponent> ent)
    {
        // load syndie base when nuke is armed
        var opts = DeserializationOptions.Default with {InitializeMaps = true};
        if (!_mapLoader.TryLoadMap(ent.Comp1.SyndieBaseMapPath, out _, out var gridSet, opts))
        {
            Log.Error($"Failed to load map from {ent.Comp1.SyndieBaseMapPath}!");
            return;
        }

        ent.Comp1.BaseGrids = gridSet.Select( x => x.Owner).ToList();
    }

    private void OnNukeExploded(NukeExplodedEvent args)
    {
        // We're just going to assume the nuke blew up in the right place.
        // That's a fair thing to assume, right? It probably won't matter
        var query = EntityQueryEnumerator<ESTraitorRuleComponent, ESTroupeRuleComponent>();
        while (query.MoveNext(out var uid, out var traitor, out var troupe))
        {
            OnNukeExploded((uid, traitor, troupe));
        }

        _roundEnd.EndRound(TimeSpan.FromMinutes(1));
    }

    private void OnNukeExploded(Entity<ESTraitorRuleComponent, ESTroupeRuleComponent> ent)
    {
        if (ent.Comp1.BaseGrids.Count <= 0)
            return;

        // Get spawn points
        var spawnPoints = new List<EntityCoordinates>();
        var query = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        while (query.MoveNext(out var spawnPoint, out var xform))
        {
            // We use latejoin spawners to indicate this is where the syndies land.
            if (spawnPoint.SpawnType != SpawnPointType.LateJoin)
                continue;

            if (xform.GridUid is null || !ent.Comp1.BaseGrids.Contains(xform.GridUid.Value))
                continue;

            spawnPoints.Add(xform.Coordinates);
        }

        if (spawnPoints.Count == 0)
            return;

        _random.Shuffle(spawnPoints);

        // Move players to spawn points
        var spawnPointIndex = 0;
        foreach (var mind in ent.Comp2.TroupeMemberMinds)
        {
            if (!TryComp<MindComponent>(mind, out var mindComp))
                continue;
            if (mindComp.OwnedEntity is not { } ownedEntity)
                continue;

            var point = spawnPoints[spawnPointIndex];
            SpawnAtPosition(ent.Comp1.TeleportEffect, Transform(ownedEntity).Coordinates); // beginning
            SpawnAtPosition(ent.Comp1.TeleportEffect, point); // destination
            _transform.SetCoordinates(ownedEntity, point);

            spawnPointIndex = (spawnPointIndex + 1) % spawnPoints.Count;
        }
    }
}
