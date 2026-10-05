using Robust.Shared.GameStates;

namespace Content.Shared._ES.Forensics.Fibers.Components;

/// <summary>
/// Used to store fibers that have been transferred to an object.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESFiberSystem))]
public sealed partial class ESFiberEvidenceComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<ESFiber> Evidence = new();
}
