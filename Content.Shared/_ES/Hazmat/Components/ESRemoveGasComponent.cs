using Content.Shared.Atmos;
using Robust.Shared.GameStates;
using Content.Shared._ES.Hazmat;

namespace Content.Shared._ES.Hazmat.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(ESSharedRemoveGasSystem))]
public sealed partial class ESRemoveGasComponent : Component
{
    [DataField]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextClean = TimeSpan.Zero;

    [DataField]
    [AutoNetworkedField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    [DataField]
    public List<Gas> GasesToRemove = new()
    {
        Gas.Miasma
    };

    [DataField]
    public float ScrubRate = 5400.0f;
}
