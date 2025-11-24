using Content.Server._ES.StationVariation.Systems;

namespace Content.Server._ES.StationVariation.Components;

/// <inheritdoc cref="ESMapLightVariationPassSystem"/>
[RegisterComponent]
public sealed partial class ESMapLightVariationPassComponent : Component
{
    [DataField(required: true)]
    public Color Light = default!;
}
