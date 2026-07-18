namespace Content.IntegrationTests.Fixtures.Attributes;

/// <summary>
///     Ensures the given test map is already made and initialized, setting <see cref="GameTest.TestMap"/> for you.
/// </summary>
/// <remarks>This only works with <see cref="GameTest"/> fixtures.</remarks>
/// <param name="testMapMode">The kind of pre-baked testmap to use.</param>
/// <seealso cref="GameTest"/>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class TestMapAttribute(TestMapMode testMapMode) : Attribute, IGameTestModifier
{
    async Task IGameTestModifier.ApplyToTest(GameTest test)
    {
        await test.CreateTestMap(testMapMode);
    }
}
