using Content.Shared.Whitelist;

namespace Content.Shared._ES.Stealth;

[RegisterComponent]
public sealed partial class StealthOnCollideComponent : Component
{
    /// <summary>
    /// Change to stealth everytime it collides with the entity
    /// </summary>
    [DataField]
    public float StealthToChange = 0.035f;

    [DataField]
    public EntityWhitelist Whitelist;

    /// <summary>
    /// Rate that effects how fast an entity's visibility passively changes.
    /// </summary>
    [DataField("passiveVisibilityRate")]
    public float PassiveVisibilityRate = -0.2f;
}
