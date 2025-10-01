using Content.Server.Administration;
using Content.Shared._ES.Cargo.Storeroom;
using Content.Shared._ES.Cargo.Storeroom.Components;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server._ES.Cargo.Storeroom;

public sealed class ESStoreroomSystem : ESSharedStoreroomSystem;

[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class ESStoreroomCommand : ToolshedCommand
{
    private ESStoreroomSystem? _storeroom;

    [CommandImplementation("viewStock")]
    public IEnumerable<string> ViewStock([PipedArgument] EntityUid station)
    {
        var comp = Comp<ESStoreroomStationComponent>(station);
        yield return "\n";
        foreach (var (container, count) in comp.Stock)
        {
            yield return $"{container.Name} x{count}";
            foreach (var content in container.Contents)
            {
                yield return $"\t{content.Name} x{content.Count}";
            }
        }
    }

    [CommandImplementation("calculateStock")]
    public IEnumerable<string> CalculateStock([PipedArgument] EntityUid station)
    {
        _storeroom ??= GetSys<ESStoreroomSystem>();

        yield return "\n";
        foreach (var (container, count) in _storeroom.GetStoreroomStock(station))
        {
            yield return $"{container.Name} x{count}";
            foreach (var content in container.Contents)
            {
                yield return $"\t{content.Name} x{content.Count}";
            }
        }
    }
}
