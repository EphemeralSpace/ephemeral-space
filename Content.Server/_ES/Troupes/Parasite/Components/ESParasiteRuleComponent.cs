namespace Content.Server._ES.Troupes.Parasite.Components;

[RegisterComponent]
[Access(typeof(ESParasiteRuleSystem))]
public sealed partial class ESParasiteRuleComponent : Component
{
    [DataField]
    public bool ObjectivesCompleted;

    /// <summary>
    /// Whether the "finale" has been triggered via all the objectives being completed and the timer passing.
    /// After this point,
    /// </summary>
    [DataField]
    public bool WinStarted;

    [DataField]
    public TimeSpan WinDelay = TimeSpan.FromMinutes(6);
}
