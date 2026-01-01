using Content.Server.Administration;
using Content.Shared._Offbrand.NuBody;
using Content.Shared.Administration;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Shared._Offbrand;

[ToolshedCommand(Name = "nurist")]
[AdminCommand(AdminFlags.Spawn)]
public sealed class NuristCommands : ToolshedCommand
{
    [Dependency] private readonly IEntityManager _entity = default!;

    [CommandImplementation("random")]
    public void Random([CommandArgument] ProtoId<SpeciesPrototype> proto, [CommandArgument(unparseable:true)] EntityCoordinates target)
    {
        _entity.System<OFMBodySystem>().SpawnRandomNurist(proto, target);
    }

    [CommandImplementation("insert_organ")]
    public void Random([CommandArgument] EntityUid body, [CommandArgument] EntityUid organ)
    {
        var container = _entity.System<SharedContainerSystem>();
        var organContainer = container.EnsureContainer<Container>(body, OFMBodyComponent.ContainerID);
        container.Insert(organ, organContainer);
    }
}
