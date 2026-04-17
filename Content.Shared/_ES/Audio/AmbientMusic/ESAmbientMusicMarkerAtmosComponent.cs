using Robust.Shared.GameStates;

namespace Content.Shared._ES.Audio.AmbientMusic;

/// <summary>
///     Marks an entity as contributing to Atmospherics ambient music.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ESAmbientMusicMarkerAtmosComponent : Component;