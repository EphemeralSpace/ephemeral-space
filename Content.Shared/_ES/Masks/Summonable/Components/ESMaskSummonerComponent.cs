using Content.Shared.Item;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Masks.Summonable.Components;

/// <summary>
/// Component that works with caches to apply <see cref="ESMaskSummonedComponent"/> to entities spawned from caches.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESMaskSummonSystem))]
public sealed partial class ESMaskSummonerComponent : Component
{
    [DataField]
    public LocId ExamineString;

    [DataField]
    public EntityWhitelist Whitelist = new()
    {
        Components = ["Item"],
    };
}
