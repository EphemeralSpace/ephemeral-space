namespace Content.Server._ES.Masks.Blessed.Components;

/// <summary>
///     Marks a mind which belongs to a Blessed mask, i.e. anyone who kills them should have
///     <see cref="ESBlessedKillerMarkerComponent"/> added to them and later be killed.
/// </summary>
[RegisterComponent]
public sealed partial class ESBlessedComponent : Component
{
    /// <summary>
    ///     it da amount time for how much time da killer has after they kill da blessed before they You Must Die (cdi ganon voice)
    /// </summary>
    [DataField]
    public TimeSpan TimeBeforeKillerDeath = TimeSpan.FromMinutes(5);
}
