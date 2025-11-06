using Content.Shared.Objectives.Components;

namespace Content.Shared._ES.Nuke.Components;

/// <summary>
/// <see cref="ObjectiveComponent"/> that is completed by compromising all <see cref="ESCryptoNukeConsoleComponent"/> on station.
/// </summary>
[RegisterComponent]
[Access(typeof(ESSharedCryptoNukeSystem))]
public sealed partial class ESCryptoNukeHackObjectiveComponent : Component;
