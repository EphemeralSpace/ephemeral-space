namespace Content.Shared._ES.Stealth;

[RegisterComponent]
public sealed partial class StealthOnCollideComponent : Component
{
    [DataField]
    public float StealthToChange = 0.5f;

    /// <summary>
    /// Rate that effects how fast an entity's visibility passively changes.
    /// </summary>
    [DataField("passiveVisibilityRate")]
    public float PassiveVisibilityRate = -0.2f;
}
