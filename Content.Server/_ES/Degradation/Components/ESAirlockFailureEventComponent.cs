namespace Content.Server._ES.Degradation.Components;

[RegisterComponent]
[Access(typeof(ESAirlockFailureEventSystem))]
public sealed partial class ESAirlockFailureEventComponent : Component
{
    /// <summary>
    /// Minimum charges to add (inclusive)
    /// </summary>
    [DataField]
    public int MinCount = 1;

    /// <summary>
    /// Maximum charges to add (inclusive)
    /// </summary>
    [DataField]
    public int MaxCount = 5;
}
