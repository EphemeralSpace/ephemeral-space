using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Cinematic;

/// <summary>
///     Defines a 'cinematic' which clients can play, i.e. things like the nuke roundend cutscene.
/// </summary>
[Prototype("esCinematic")]
public sealed partial class ESCinematicPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     The sprite to render as a texture over the viewport for the duration of the cinematic.
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier Animation = default!;

    /// <summary>
    ///     Total length of the cinematic.
    ///     If this is shorter than the animation length, the animation will be cut off at that time.
    ///     If it's longer than the animation length, the animation will loop.
    /// </summary>
    [DataField(required: true)]
    public TimeSpan Length;

    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    /// <summary>
    ///     Time override for the curtain animation.
    ///     If this is null, no curtains will play.
    ///     If this is non-null, the curtain close anim will play 2x this amount of seconds before the cinematic ends.
    ///     An open animation will not play. The caller is in charge of ensuring this happens at some point (from round end etc.)
    ///     or else the auto anim will just play.
    /// </summary>
    [DataField]
    public TimeSpan? CurtainLength;
}
