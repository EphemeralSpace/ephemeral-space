namespace Content.Shared._ES.Auditions.Components;

/// <summary>
/// Tracks social groups in the cast. This can be used for tracking crews, workers unions, friend groups, and more.
/// </summary>
[RegisterComponent]
public sealed partial class ESSocialGroupComponent : Component
{
    /// <summary>
    /// Who are the members in this social group?
    /// </summary>
    [DataField]
    public List<EntityUid> Members = new ();

    /// <summary>
    /// When generating relationships within this group, what relationship context should we use?
    /// </summary>
    [DataField]
    public ESRelationshipContext RelativeContext = new ();

    /// <summary>
    /// Has this social group been integrated already?
    /// </summary>
    [DataField]
    public bool Integrated;
}
