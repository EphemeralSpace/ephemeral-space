using Content.IntegrationTests.Tests._Citadel.Attributes;
using Robust.Shared.Configuration;

namespace Content.IntegrationTests.Tests._Citadel.TestTests;

[Explicit("Manually run by NUnit tooling, only run this yourself if you're debugging.")]
public sealed class EnsureCVarTest : GameTest
{
    [SidedDependency(Side.Server)] private readonly IConfigurationManager _sCfgMan = default!;

    [Test]
    [Description("Ensure that EnsureCVar performs its usual operations.")]
    [EnsureCVar(Side.Server, typeof(TestCVarDefs), nameof(TestCVarDefs.TestCVar), "bar")]
    [RunOnSide(Side.Server)]
    public void EnsureCVarIsBar()
    {
        Assert.That(_sCfgMan.GetCVar(TestCVarDefs.TestCVar), Is.EqualTo("bar"));
    }
}
