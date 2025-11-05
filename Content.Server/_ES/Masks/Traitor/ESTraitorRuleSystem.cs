using Content.Server._ES.Masks.Traitor.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Nuke;
using Content.Server.RoundEnd;
using Content.Server.Spawners.Components;
using Content.Shared._ES.Masks.Components;
using Content.Shared.Mind;
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
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESTraitorRuleComponent, RuleLoadedGridsEvent>(OnRuleLoadedGrids);

        SubscribeLocalEvent<NukeExplodedEvent>(OnNukeExploded);
    }

    private void OnRuleLoadedGrids(Entity<ESTraitorRuleComponent> ent, ref RuleLoadedGridsEvent args)
    {
        ent.Comp.BaseGrids.AddRange(args.Grids);
    }

    private void OnNukeExploded(NukeExplodedEvent args)
    {
        // We're just going to assume the nuke blew up in the right place.
        // That's a fair thing to assume, right? It probably won't matter.

        var query = EntityQueryEnumerator<ESTraitorRuleComponent, ESTroupeRuleComponent, LoadMapRuleComponent>();
        while (query.MoveNext(out var uid, out var traitor, out var troupe, out var map))
        {
            OnNukeExploded((uid, traitor, troupe, map));
        }

        // TODO: Mark troupe victory
        _roundEnd.EndRound(TimeSpan.FromMinutes(1));
    }

    public void OnNukeExploded(Entity<ESTraitorRuleComponent, ESTroupeRuleComponent, LoadMapRuleComponent> ent)
    {
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
            _transform.SetCoordinates(ownedEntity, point);
            SpawnAtPosition(ent.Comp1.TeleportEffect, point);

            spawnPointIndex = (spawnPointIndex + 1) % spawnPoints.Count;
        }
    }
}
