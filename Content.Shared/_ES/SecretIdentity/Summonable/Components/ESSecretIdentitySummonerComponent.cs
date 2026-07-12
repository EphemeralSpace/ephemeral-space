using Content.Shared.Item;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.SecretIdentity.Summonable.Components;

/// <summary>
/// Component that works with caches to apply <see cref="ESSecretIdentitySummonedComponent"/> to entities spawned from caches.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSecretIdentitySummonSystem))]
public sealed partial class ESSecretIdentitySummonerComponent : Component
{
    [DataField]
    public LocId ExamineString;

    [DataField]
    public EntityWhitelist Whitelist = new()
    {
        Components = ["Item"],
    };
}
