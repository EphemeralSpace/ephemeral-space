using Content.Shared.Atmos;

namespace Content.Server.NodeContainer.Nodes;

// todo some day actually make disposals shit use nodes
// this is just for nodecrawling basically, but disposals should always have functioned like this to begin with.
[DataDefinition]
public sealed partial class DisposalsTubeNode : PipeNode
{
    public override GasMixture Air
    {
        get
        {
            // return default air while crawling in disposals tube. just dont think about it that hard
            var mix = new GasMixture(Atmospherics.CellVolume) { Temperature = Atmospherics.T20C };
            mix.AdjustMoles(Gas.Oxygen, Atmospherics.OxygenMolesStandard);
            mix.AdjustMoles(Gas.Nitrogen, Atmospherics.NitrogenMolesStandard);
            return mix;
        }
        set {}
    }
}
