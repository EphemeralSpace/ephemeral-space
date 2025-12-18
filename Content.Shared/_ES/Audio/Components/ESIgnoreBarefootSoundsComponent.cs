using Robust.Shared.GameStates;

namespace Content.Shared._ES.Audio.Components;

/// <summary>
/// Marker component specifically for disabling barefoot sounds when walking.
/// When present, the sounds will ALWAYS be the shoe-on ones, and never barefoot.
/// plap plap plap plap plap little ants are running around on my stage and driving me nuts
/// I lost my goddamn mind listening to their wet-ass feet slappign all across the wood
/// I can hear my heart beat i can hear my heart beat i can feel the world beat
/// on and on on and on and on on and on as one
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ESIgnoreBarefootSoundsComponent : Component;
