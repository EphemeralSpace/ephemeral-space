using Content.Shared.Atmos;

namespace Content.Server.NodeContainer.Nodes;

// todo some day actually make disposals shit use nodes
// this is just for nodecrawling basically, but disposals should always have functioned like this to begin with.
[DataDefinition]
public sealed partial class DisposalsTubeNode : PipeNode
{
    // actual air getting is handled by the node crawler itself taking air from the atmosphere.
    public override GasMixture Air => GasMixture.SpaceGas;
}
