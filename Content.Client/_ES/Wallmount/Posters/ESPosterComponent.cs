using Robust.Shared.Serialization;

namespace Content.Client._ES.Wallmount.Posters;


/// <summary>
/// Exists because posters currently just set a single state at the base of spritecomp
/// and because we want directional posters for directionality purposes
/// </summary>
[RegisterComponent]
public sealed partial class ESPosterComponent : Component
{
    // The state to set the poster key to.
    [DataField(required: true)]
    public string State = default!;
}

[Serializable]
public enum ESPosterVisuals : byte
{
    Base
}
