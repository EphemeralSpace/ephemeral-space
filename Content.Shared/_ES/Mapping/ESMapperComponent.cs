using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Mapping;

[RegisterComponent, NetworkedComponent, Access(typeof(ESSharedSelectionSystem))]
public sealed partial class ESMapperComponent : Component
{
    [DataField]
    public ESSelectionState SelectionState = new ESSelectionState.Disabled();

    [DataField]
    public ESSelectionBox? ActiveGridSelection;

    [DataField]
    public HashSet<EntityUid>? ActiveEntitySelection;
}

[Serializable, NetSerializable]
public sealed class ESMapperComponentState : IComponentState
{
    public ESNetSelectionState SelectionState = default!;
    public ESNetSelectionBox? ActiveGridSelection = default!;
    public HashSet<NetEntity>? ActiveEntitySelection = default!;
}

[ImplicitDataDefinitionForInheritors]
public abstract partial record ESSelectionState
{
    public sealed partial record Disabled : ESSelectionState;
    public sealed partial record Idle : ESSelectionState;
    public sealed partial record Selecting : ESSelectionState
    {
        public readonly ESSelectionBox Selection;

        public Selecting(ESSelectionBox selection)
        {
            Selection = selection;
        }
    }
    public sealed partial record Moving : ESSelectionState
    {
        public readonly ESLine Line;

        public Moving(ESLine line)
        {
            Line = line;
        }
    }
}

[Serializable, NetSerializable]
public abstract record ESNetSelectionState
{
    [Serializable, NetSerializable]
    public sealed record Disabled : ESNetSelectionState;

    [Serializable, NetSerializable]
    public sealed record Idle : ESNetSelectionState;

    [Serializable, NetSerializable]
    public sealed record Selecting : ESNetSelectionState
    {
        public readonly ESNetSelectionBox Selection;

        public Selecting(ESNetSelectionBox selection)
        {
            Selection = selection;
        }
    }

    [Serializable, NetSerializable]
    public sealed record Moving : ESNetSelectionState
    {
        public readonly ESNetLine Line;

        public Moving(ESNetLine line)
        {
            Line = line;
        }
    }
}

[DataDefinition]
public readonly partial record struct ESSelectionBox(
    [property: ViewVariables]
    EntityUid RelativeTo,
    [property: ViewVariables]
    Vector2 Start,
    [property: ViewVariables]
    Vector2 End,
    [property: ViewVariables]
    Angle Angle);

[Serializable, NetSerializable]
public readonly record struct ESNetSelectionBox(
    NetEntity RelativeTo,
    Vector2 Start,
    Vector2 End,
    Angle Angle);

[DataDefinition]
public readonly partial record struct ESLine(
    [property: ViewVariables]
    EntityUid RelativeTo,
    [property: ViewVariables]
    Vector2 Start,
    [property: ViewVariables]
    Vector2 End);

[Serializable, NetSerializable]
public readonly record struct ESNetLine(
    NetEntity RelativeTo,
    Vector2 Start,
    Vector2 End);

[Serializable, NetSerializable]
public sealed class ESMappingDisableSelectionMessage : ESMappingMessage;

[Serializable, NetSerializable]
public sealed class ESMappingEnableSelectionMessage : ESMappingMessage;

[Serializable, NetSerializable]
public sealed class ESMappingStartSelectionMessage(NetCoordinates startPosition, Angle angle) : ESMappingMessage
{
    public readonly NetCoordinates StartPosition = startPosition;
    public readonly Angle Angle = angle;
}

[Serializable, NetSerializable]
public sealed class ESMappingMidSelectionMessage(NetCoordinates endPosition) : ESMappingMessage
{
    public readonly NetCoordinates EndPosition = endPosition;
}

[Serializable, NetSerializable]
public sealed class ESMappingStartMovingMessage(NetCoordinates startPosition) : ESMappingMessage
{
    public readonly NetCoordinates StartPosition = startPosition;
}

[Serializable, NetSerializable]
public sealed class ESMappingMidMovingMessage(NetCoordinates endPosition) : ESMappingMessage
{
    public readonly NetCoordinates EndPosition = endPosition;
}

[Serializable, NetSerializable]
public sealed class ESMappingStopMovingMessage : ESMappingMessage;

[Serializable, NetSerializable]
public sealed class ESMappingStopSelectionMessage : ESMappingMessage;
