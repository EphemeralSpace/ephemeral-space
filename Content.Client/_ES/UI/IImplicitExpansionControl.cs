namespace Content.Client._ES.UI;

/// <summary>
///     Marker interface for Stack.Expansion to know it should warn about a type implicitly already
///     setting Expansion.
/// </summary>
public interface IImplicitExpansionControl
{
    /// <summary>
    ///     The type to use in place of this control if you want explicit expansions.
    /// </summary>
    Type PreferredType { get; }
}

