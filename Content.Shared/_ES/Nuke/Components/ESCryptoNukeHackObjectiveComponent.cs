using Content.Shared._ES.Objectives.Components;

namespace Content.Shared._ES.Nuke.Components;

/// <summary>
/// <see cref="ESObjectiveComponent"/> that is completed by compromising all <see cref="ESCryptoNukeConsoleComponent"/> on station.
/// </summary>
[RegisterComponent]
[Access(typeof(ESSharedCryptoNukeSystem))]
public sealed partial class ESCryptoNukeHackObjectiveComponent : Component;
