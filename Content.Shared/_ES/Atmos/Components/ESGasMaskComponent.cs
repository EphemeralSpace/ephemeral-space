using Content.Shared.Atmos;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Atmos.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedGasMaskSystem))]
public sealed partial class ESGasMaskComponent : Component
{
    [DataField]
    public List<Gas> BlockedGases = new()
    {
        Gas.Smoke,
        Gas.Miasma,
        Gas.Cryogas,
    };
}
