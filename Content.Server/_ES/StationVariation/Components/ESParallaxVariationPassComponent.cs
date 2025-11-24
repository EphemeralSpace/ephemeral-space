using Content.Server._ES.StationVariation.Systems;

namespace Content.Server._ES.StationVariation.Components;

/// <inheritdoc cref="ESParallaxVariationPassSystem"/>
[RegisterComponent]
public sealed partial class ESParallaxVariationPassComponent : Component
{
    // this cant be a protoid because uhh parallaxprototype is client only and that is annoying to deal with lol
    [DataField(required: true)]
    public string Parallax = default!;
}
