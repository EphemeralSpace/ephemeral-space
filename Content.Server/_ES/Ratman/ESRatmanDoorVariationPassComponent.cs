using Robust.Shared.Prototypes;

namespace Content.Server._ES.Ratman;

/// <inheritdoc cref="ESRatmanDoorVariationPassSystem"/>
[RegisterComponent]
public sealed partial class ESRatmanDoorVariationPassComponent : Component
{
    [DataField]
    public EntProtoId Replacement = "SolidSecretDoorRatman";

    [DataField]
    public int Count = 6;
}
