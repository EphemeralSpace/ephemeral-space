using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Cryohusk.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedCryohuskSystem))]
public sealed partial class ESCryohuskableOrganComponent : Component
{
    [DataField(required: true)]
    public EntProtoId CryohusksInto;
}

[ByRefEvent]
public readonly record struct ESGotCryohuskedEvent;
