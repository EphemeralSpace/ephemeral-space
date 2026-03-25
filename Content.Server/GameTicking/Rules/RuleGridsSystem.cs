using Robust.Shared.Map;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Handles storing grids from <see cref="RuleLoadedGridsEvent"/>
/// </summary>
public sealed class RuleGridsSystem : GameRuleSystem<RuleGridsComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GridSplitEvent>(OnGridSplit);

        SubscribeLocalEvent<RuleGridsComponent, RuleLoadedGridsEvent>(OnLoadedGrids);
    }

    private void OnGridSplit(ref GridSplitEvent args)
    {
        var rule = QueryActiveRules();
        while (rule.MoveNext(out _, out var comp, out _))
        {
            if (!comp.MapGrids.Contains(args.Grid))
                continue;

            comp.MapGrids.AddRange(args.NewGrids);
            break; // only 1 rule can own a grid, not multiple
        }
    }

    private void OnLoadedGrids(Entity<RuleGridsComponent> ent, ref RuleLoadedGridsEvent args)
    {
        var (uid, comp) = ent;
        if (comp.Map != null && args.Map != comp.Map)
        {
            Log.Warning($"{ToPrettyString(uid):rule} loaded grids on multiple maps {comp.Map} and {args.Map}, the second will be ignored.");
            return;
        }

        comp.Map = args.Map;
        comp.MapGrids.AddRange(args.Grids);
    }
}

/// <summary>
/// Raised by another gamerule system to store loaded grids, and have other systems work with it.
/// A single rule can only load grids for a single map, attempts to load more are ignored.
/// </summary>
[ByRefEvent]
public record struct RuleLoadedGridsEvent(MapId Map, IReadOnlyList<EntityUid> Grids);
