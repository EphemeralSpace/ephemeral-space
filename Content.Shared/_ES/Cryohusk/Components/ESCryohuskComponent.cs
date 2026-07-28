using Robust.Shared.GameStates;

namespace Content.Shared._ES.Cryohusk.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedCryohuskSystem))]
public sealed partial class ESCryohuskComponent : Component
{
    [DataField]
    public float SpeedModifier = 0.9f;
}
