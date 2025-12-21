namespace Content.Server._ES.Masks.Martyr.Components;

/// <summary>
///     Marks a mind which belongs to a Martyr mask, i.e. anyone who kills them should have
///     <see cref="ESMartyrKillerMarkerComponent"/> added to them and later be killed.
/// </summary>
[RegisterComponent]
public sealed partial class ESMartyrComponent : Component
{
    /// <summary>
    ///     it da amount time for how much time da killer has after they kill da martyr before they You Must Die (cdi ganon voice)
    /// </summary>
    [DataField]
    public TimeSpan TimeBeforeKillerDeath = TimeSpan.FromMinutes(5);
}
