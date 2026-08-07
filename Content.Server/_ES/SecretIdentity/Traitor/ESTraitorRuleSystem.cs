using System.Linq;
using Content.Server._ES.SecretIdentity.Traitor.Components;
using Content.Server.GameTicking;
using Content.Server.Nuke;
using Content.Server.RoundEnd;
using Content.Server.Spawners.Components;
using Content.Shared._ES.Cinematic;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.SecretIdentity.Components;
using Content.Shared.Mind;
using Robust.Server.Audio;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.SecretIdentity.Traitor;

/// <summary>
/// This handles <see cref="ESTraitorRuleComponent"/>
/// </summary>
public sealed partial class ESTraitorRuleSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private RoundEndSystem _roundEnd = default!;
    [Dependency] private MapLoaderSystem _mapLoader = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private ESEntityTimerSystem _timer = default!;
    [Dependency] private ESCinematicSystem _cinematic = default!;

    /// <summary>
    ///     Round will actually end (screen pops up music plays etc) this amount of time before the cinematic finishes.
    /// </summary>
    private static readonly TimeSpan EndRoundDuration = TimeSpan.FromSeconds(13);
    private static readonly ProtoId<ESCinematicPrototype> NukeCinematic = "NukeCinematic";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<NukeArmedEvent>(OnNukeArmed);
        SubscribeLocalEvent<ESNukePreExplosionEvent>(OnNukePreExplosion);
        SubscribeLocalEvent<ESNukeAfterExplodedEvent>(OnNukeExploded);
    }

    private void OnNukeArmed(NukeArmedEvent ev)
    {
        var query = EntityQueryEnumerator<ESTraitorRuleComponent, ESOrganizationRuleComponent>();
        while (query.MoveNext(out var uid, out var traitor, out var organization))
        {
            OnNukeArmed((uid, traitor, organization));
        }
    }

    private void OnNukeArmed(Entity<ESTraitorRuleComponent, ESOrganizationRuleComponent> ent)
    {
        // load syndie base when nuke is armed
        var opts = DeserializationOptions.Default with {InitializeMaps = true};
        if (!_mapLoader.TryLoadMap(ent.Comp1.SyndieBaseMapPath, out var map, out var gridSet, opts))
        {
            Log.Error($"Failed to load map from {ent.Comp1.SyndieBaseMapPath}!");
            return;
        }

        ent.Comp1.SyndieBaseMapId = map.Value.Comp.MapId;
        ent.Comp1.BaseGrids = gridSet.Select( x => x.Owner).ToList();
    }

    private void OnNukePreExplosion(ref ESNukePreExplosionEvent ev)
    {
        var query = EntityQueryEnumerator<ESTraitorRuleComponent, ESOrganizationRuleComponent>();
        while (query.MoveNext(out var uid, out var traitor, out var organization))
        {
            OnNukePreExploded((uid, traitor, organization));
        }
    }

    private void OnNukeExploded(ref ESNukeAfterExplodedEvent args)
    {
        // We're just going to assume the nuke blew up in the right place.
        // That's a fair thing to assume, right? It probably won't matter

        // play cinematic for everyone
        var filter = Filter.Broadcast();
        var cinematic = ProtoMan.Index(NukeCinematic);
        _cinematic.PlayCinematic(NukeCinematic, filter);
        _timer.SpawnMethodTimer(cinematic.Length - EndRoundDuration,
            () =>
            {
                _roundEnd.EndRound(EndRoundDuration);
            });

        // pause station map
        _map.SetPaused(_ticker.DefaultMap, true);
    }

    private void OnNukePreExploded(Entity<ESTraitorRuleComponent, ESOrganizationRuleComponent> ent)
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
        foreach (var mind in ent.Comp2.OrganizationMemberMinds)
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
