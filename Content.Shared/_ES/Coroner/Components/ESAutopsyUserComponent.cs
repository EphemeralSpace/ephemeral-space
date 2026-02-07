using Robust.Shared.GameStates;

namespace Content.Shared._ES.Coroner.Components;

/// <summary>
/// Marks a character as being a coroner and able to use <see cref="ESAutopsyToolComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedCoronerSystem))]
public sealed partial class ESAutopsyUserComponent : Component;
