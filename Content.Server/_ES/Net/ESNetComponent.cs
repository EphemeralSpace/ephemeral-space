using Robust.Shared.Audio;

namespace Content.Server._ES.Net;

/// <summary>
///     An item which, when thrown, will attempt to yoink an item out of the hands of anyone it hits.
/// </summary>
[RegisterComponent]
public sealed partial class ESNetComponent : Component
{
    /// <summary>
    ///     Chance that the net will catch the item.
    /// </summary>
    [DataField]
    public float Chance = 1.0f;

    /// <summary>
    ///     Sound to play when an item is snatched.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;
}
