using Content.Shared.Whitelist;

namespace Content.Shared._ES.Stealth;

[RegisterComponent]
public sealed partial class StealthOnCollideComponent : Component
{
    [DataField]
    public float StealthToChange = 0.1f;

    [DataField]
    public EntityWhitelist Whitelist;

    /// <summary>
    /// Rate that effects how fast an entity's visibility passively changes.
    /// </summary>
    [DataField("passiveVisibilityRate")]
    public float PassiveVisibilityRate = -0.2f;
}
