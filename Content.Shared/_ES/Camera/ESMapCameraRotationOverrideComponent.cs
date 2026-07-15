using Content.Shared.Movement.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Camera;

/// <summary>
///     Component which goes on map entities.
///     Any <see cref="InputMoverComponent"/>s which are on this map
///     will have their camera rotation locked to <see cref="RotationOverride"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ESMapCameraRotationOverrideComponent : Component
{
    [DataField, AutoNetworkedField]
    public Angle RotationOverride;
}
