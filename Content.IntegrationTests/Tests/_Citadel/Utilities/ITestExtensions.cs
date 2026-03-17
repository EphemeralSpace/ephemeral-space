using NUnit.Framework.Interfaces;

namespace Content.IntegrationTests.Tests._Citadel.Utilities;

public static class ITestExtensions
{
    extension<T>(T test)
        where T: ITest
    {
        public void EnsureFixtureIsGameTest(Type callingType, out GameTest gt)
        {
            if (test.Fixture is not GameTest gameTest)
            {
                throw new NotSupportedException(
                    $"The fixture {test.Fixture?.GetType()} needs to be a GameTest for {callingType} to work.");
            }

            gt = gameTest;
        }
    }
}
