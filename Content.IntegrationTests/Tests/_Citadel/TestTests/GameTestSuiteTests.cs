using Content.IntegrationTests.Tests._Citadel.Attributes;
using Robust.Shared.Configuration;

namespace Content.IntegrationTests.Tests._Citadel.TestTests;

[EnsureCVar(Side.Server, typeof(TestCVarDefs), nameof(TestCVarDefs.TestCVar), "bar")]
[TestMap(TestMapMode.Arena)]
public sealed class GameTestSuiteTests : GameTest
{
    [SidedDependency(Side.Server)] private readonly IConfigurationManager _sCfgMan = default!;

    [Test]
    [Description("Ensure the suite-wide EnsureCVar applies.")]
    public void EnsureCVarIsBar()
    {
        Assert.That(_sCfgMan.GetCVar<string>(TestCVarDefs.TestCVar), Is.EqualTo("bar"));
    }

    [Test]
    [Description("Ensure the suite-wide TestMap applies.")]
    public void EnsureTestMap()
    {
        Assert.That(TestMap, Is.Not.Null);
    }
}
