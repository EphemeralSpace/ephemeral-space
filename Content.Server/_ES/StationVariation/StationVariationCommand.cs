using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Content.Shared.Station.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server._ES.StationVariation;

[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class StationVariationCommand : ToolshedCommand
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    private GameTicker? _ticker;

    [CommandImplementation("runPass")]
    public void VariationRunPass(string rule)
    {
        _ticker ??= GetSys<GameTicker>();

        var variationRule = _ticker.AddGameRule(rule);

        var stationQuery = EntityManager.EntityQueryEnumerator<StationDataComponent>();
        while (stationQuery.MoveNext(out var uid, out var data))
        {
            var ev = new StationVariationPassEvent((uid, data));
            EntityManager.EventBus.RaiseLocalEvent(variationRule, ref ev);
        }

        _ticker.EndGameRule(variationRule);
    }
}
