using Content.IntegrationTests.Tests._Citadel.NUnit;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;

namespace Content.IntegrationTests.Tests._Citadel.TestTests;

public sealed class EnsureCVarMetaTest
{
    [Test]
    [Description($"Runs {nameof(EnsureCVarTest)} and ensures the pair is still clean.")]
    public void RunEnsureCVarTestTestFixture()
    {
        var fixture = new EnsureCVarTest();

        // Create work..
        var workItem = TestBuilder.CreateWorkItem(typeof(EnsureCVarTest), nameof(EnsureCVarTest.EnsureCVarIsBar), fixture);

        fixture.PreFinalizeHook += () =>
        {
            // Check on our fixture.
            fixture.Pair.Server.WaitAssertion(() =>
            {
                var cfg = IoCManager.Resolve<IConfigurationManager>();

                Assert.That(cfg.GetCVar(TestCVarDefs.TestCVar), Is.EqualTo("foo"));
            })
                .Wait();
        };

        // Do work.
        TestBuilder.ExecuteWorkItem(workItem);

        Assert.That(workItem.Result.FailCount, Is.Zero, $"Inner test failed: {workItem.Result.Message}");
    }
}
