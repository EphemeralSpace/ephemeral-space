using Robust.Shared.GameStates;

namespace Content.Shared._ES.Coroner.Components;

/// <summary>
/// Marks a character as being a coroner and able to use <see cref="ESCoronerToolComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedCoronerSystem))]
public sealed partial class ESCoronerUserComponent : Component;
