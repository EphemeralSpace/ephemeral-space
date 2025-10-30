using System.Linq;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Core.Entity;

public static class SpawnHelpers
{
    [PublicAPI]
    public static IEnumerable<EntityUid> SpawnAtPosition(
        this IEntityManager entityManager,
        List<EntProtoId> prototypes,
        EntityCoordinates coords,
        ESSpawnStrategy strategy,
        float offset = 0.2f)
    {
        return entityManager.SpawnAtPosition(
            prototypes.Select(e => (EntProtoId?) e).ToList(),
            coords,
            strategy,
            offset);
    }

    [PublicAPI]
    public static IEnumerable<EntityUid> SpawnAtPosition(
        this IEntityManager entityManager,
        List<EntProtoId?> prototypes,
        EntityCoordinates coords,
        ESSpawnStrategy strategy,
        float offset = 0.2f)
    {
        var outEnts = new List<EntityUid>();
        switch (strategy)
        {
            case ESSpawnStrategy.Random:
                var rand = new System.Random();
                foreach (var prototype in prototypes)
                {
                    var xOffset = rand.NextFloat(-offset, offset);
                    var yOffset = rand.NextFloat(-offset, offset);
                    var trueCoords = coords.Offset(new Vector2(xOffset, yOffset));

                    outEnts.Add(entityManager.SpawnAtPosition(prototype, trueCoords));
                }
                break;
            case ESSpawnStrategy.Horizontal:
                var hStartPos = coords.Offset(new Vector2(-offset, 0));
                var hEndPos = coords.Offset(new Vector2(offset, 0));
                outEnts.AddRange(entityManager.SpawnAtPositionLinear(prototypes, hStartPos, hEndPos));
                break;
            case ESSpawnStrategy.Vertical:
                var vStartPos = coords.Offset(new Vector2(0, -offset));
                var vEndPos = coords.Offset(new Vector2(0, offset));
                outEnts.AddRange(entityManager.SpawnAtPositionLinear(prototypes, vStartPos, vEndPos));
                break;
            case ESSpawnStrategy.Diagonal:
                var dStartPos = coords.Offset(new Vector2(-offset, -offset));
                var dEndPos = coords.Offset(new Vector2(offset, offset));
                outEnts.AddRange(entityManager.SpawnAtPositionLinear(prototypes, dStartPos, dEndPos));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null);
        }
        return outEnts;
    }

    /// <summary>
    /// Spawns a list of entities along a line defined from startPos to endPos
    /// If there's only one entity, it'll spawn in the center of the line.
    /// </summary>
    public static IEnumerable<EntityUid> SpawnAtPositionLinear(
        this IEntityManager entityManager,
        List<EntProtoId?> prototypes,
        EntityCoordinates startPos,
        EntityCoordinates endPos)
    {
        DebugTools.Assert(startPos.EntityId == endPos.EntityId);

        if (prototypes.Count == 0)
            yield break;

        // Special Case: If there is only one entity, spawn it in the center
        if (prototypes.Count == 1)
        {
            var midX = (startPos.X + endPos.X) / 2;
            var midY = (startPos.Y + endPos.Y) / 2;
            yield return entityManager.SpawnAtPosition(prototypes[0], new EntityCoordinates(startPos.EntityId, midX, midY));
            yield break; // Exit
        }

        for (var i = 0; i < prototypes.Count; i++)
        {
            // Get a blend value corresponding to how far along we are in the list
            var lerp = (float) i / (prototypes.Count - 1);
            var x = MathHelper.Lerp(startPos.X, endPos.X, lerp);
            var y = MathHelper.Lerp(startPos.Y, endPos.Y, lerp);
            var coords = new EntityCoordinates(startPos.EntityId, x, y);
            yield return entityManager.SpawnAtPosition(prototypes[i], coords);
        }
    }
}
