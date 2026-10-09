using Content.Shared.PrototypeTable.PrototypeSelectors;
using Robust.Shared.Prototypes;

namespace Content.Shared.Containers;

/// <summary>
/// Version of <see cref="ContainerFillComponent"/> that utilizes <see cref="PrototypeTableSelector{EntityPrototype}"/>
/// </summary>
[RegisterComponent, Access(typeof(ContainerFillSystem))]
public sealed partial class EntityTableContainerFillComponent : Component
{
    [DataField]
    public Dictionary<string, PrototypeTableSelector<EntityPrototype>> Containers = new();
}
