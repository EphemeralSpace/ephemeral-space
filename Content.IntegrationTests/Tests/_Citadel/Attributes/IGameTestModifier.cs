namespace Content.IntegrationTests.Tests._Citadel.Attributes;

/// <summary>
///     Marks an attribute as a modifier for <see cref="GameTest"/> fixtures.
/// </summary>
public interface IGameTestModifier
{
    Task ApplyToTest(GameTest test);
}

