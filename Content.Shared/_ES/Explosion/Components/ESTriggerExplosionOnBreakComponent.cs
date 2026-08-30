using Robust.Shared.GameStates;

namespace Content.Shared._ES.Explosion.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESTriggerExplosionOnBreakComponent))]
public sealed partial class ESTriggerExplosionOnBreakComponent : Component;
