using System.Linq;
using Content.Shared._ES.Telesci.Anomaly;
using Content.Shared._ES.Telesci.Anomaly.Components;
using Robust.Server.GameObjects;

namespace Content.Server._ES.Telesci.Anomaly;

/// <inheritdoc/>
public sealed class ESAnomalySystem : ESSharedAnomalySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void UpdateUi(Entity<ESAnomalyConsoleComponent?, UserInterfaceComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return;

        var state = new ESAnomalyConsoleBuiState();

        foreach (var anomaly in ent.Comp1.Anomalies)
        {
            if (!TryComp<ESPortalAnomalyComponent>(anomaly, out var comp))
                continue;

            var name = Name(anomaly);
            var visibleSignals = Math.Min(comp.CodeIndex + 1, comp.CodeLength);
            var signals = comp.SignalCode.Take(visibleSignals).ToList();
            var length = comp.CodeLength;
            state.Anomalies.Add((name, signals, length));
        }

        _ui.SetUiState((ent, ent.Comp2), ESAnomalyConsoleUiKey.Key, state);
    }
}
