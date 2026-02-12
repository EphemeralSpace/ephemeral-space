using Robust.Shared.GameStates;

namespace Content.Shared._ES.Coroner.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedCoronerSystem))]
public sealed partial class ESAutopsyTableComponent : Component;
