using Content.Shared.CrewManifest;
using Robust.Shared.GameStates;

namespace Content.Shared.CrewManifest;

/// <summary>
/// Component which replicates the crew manifest onto clients.
/// This component is supposed to be attached to a station.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CrewManifestComponent : Component
{
    [DataField, AutoNetworkedField]
    public CrewManifestEntries? Entries;
}