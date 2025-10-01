using Content.Shared.Construction.Components;
using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Construction.NodeEntities;

/// <summary>
///     Works for both <see cref="ComputerBoardComponent"/> and <see cref="MachineBoardComponent"/>
///     because duplicating code just for this is really stinky.
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed partial class BoardNodeEntity : IGraphNodeEntity
{
    [DataField]
    public string Container { get; private set; } = string.Empty;

// ES START
    [DataField]
    public ProtoId<TagPrototype>? AlternateComputerTag;
// ES END

    public string? GetId(EntityUid? uid, EntityUid? userUid, GraphNodeEntityArgs args)
    {
        if (uid == null)
            return null;

        var containerSystem = args.EntityManager.EntitySysManager.GetEntitySystem<SharedContainerSystem>();

        if (!containerSystem.TryGetContainer(uid.Value, Container, out var container)
            || container.ContainedEntities.Count == 0)
            return null;

        var board = container.ContainedEntities[0];

        // There should not be a case where more than one of these components exist on the same entity
        if (args.EntityManager.TryGetComponent(board, out MachineBoardComponent? machine))
            return machine.Prototype;

// ES START
        if (args.EntityManager.TryGetComponent(board, out ComputerBoardComponent? computer))
        {
            if (AlternateComputerTag is { } tag && computer.AlternatePrototype.TryGetValue(tag, out var proto))
                return proto;
            return computer.Prototype;
        }
// ES END

        if (args.EntityManager.TryGetComponent(board, out ElectronicsBoardComponent? electronics))
            return electronics.Prototype;

        return null;
    }
}
