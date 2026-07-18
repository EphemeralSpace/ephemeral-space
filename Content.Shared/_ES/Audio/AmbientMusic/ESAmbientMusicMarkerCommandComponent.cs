using Robust.Shared.GameStates;

namespace Content.Shared._ES.Audio.AmbientMusic;

/// <summary>
///     Marks an entity as contributing to Command ambient music.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ESAmbientMusicMarkerCommandComponent : Component;
