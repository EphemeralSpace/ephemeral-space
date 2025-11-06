using Robust.Shared.GameStates;

namespace Content.Shared._ES.Nuke.Components;

/// <summary>
/// Used to track objectives that serve as prerequisites to being able to compromise the <see cref="ESCryptoNukeConsoleComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedCryptoNukeSystem))]
public sealed partial class ESNukePrereqObjectiveComponent : Component;
