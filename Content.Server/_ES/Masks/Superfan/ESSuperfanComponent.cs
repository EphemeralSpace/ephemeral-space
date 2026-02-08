namespace Content.Server._ES.Masks.Superfan;

/// <summary>
///     This is used for the syndie superfan and their conversion on traitor loss.
/// </summary>
/// <remarks>
///     Deliberately not generalized, as writing general code here is a bunch of extra tests and work that may never
///     be used. I like it when my language can do the typechecking for me instead of needing to test for it.
///
///     If we ever need equivalents for like, nihlings, this should not be hard to rewrite.
/// </remarks>
[RegisterComponent]
public sealed partial class ESSuperfanComponent : Component;
