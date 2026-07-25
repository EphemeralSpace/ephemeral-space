namespace Content.Server.Speech.Components;

[RegisterComponent]
public sealed partial class SlurredAccentComponent : Component
{
    [DataField]
    public float Probability = 0.5f;
}
