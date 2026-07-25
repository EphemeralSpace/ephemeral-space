using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Cryohusk.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ESCryohuskIdCardComponent : Component
{
    [DataField]
    public SpriteSpecifier Overlay = new SpriteSpecifier.Rsi(new ResPath("_ES/Objects/Misc/identification_cards.rsi"), "frozen");

    [DataField]
    public ProtoId<JobIconPrototype> JobIcon = "ESJobIconCryohusk";
}
