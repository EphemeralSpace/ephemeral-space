using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace Content.IntegrationTests.Tests._Citadel;

/// <summary>
///     Ensures a test method runs on the given side (client or server) when used with a <see cref="GameTest"/> fixture.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RunOnSideAttribute : Attribute, IWrapTestMethod, IImplyFixture
{
    /// <summary>
    ///     Which side to run the inner test code on, if not the test thread.
    /// </summary>
    public Side RunOnSide { get; set; }

    public RunOnSideAttribute(Side side)
    {
        RunOnSide = side;
    }

    public TestCommand Wrap(TestCommand command)
    {
        return new SidedTestCommand(command, RunOnSide);
    }

    private sealed class SidedTestCommand : DelegatingTestCommand
    {
        private Side _side;

        public SidedTestCommand(TestCommand inner, Side side) : base(inner)
        {
            _side = side;
        }

        public override TestResult Execute(TestExecutionContext context)
        {
            if (innerCommand.Test.Fixture is not GameTest gt)
            {
                throw new NotSupportedException(
                    $"The fixture {innerCommand.Test.Fixture!.GetType()} needs to be a GameTest for SidedTest to work.");
            }

            if (_side is Side.Neither)
                throw new NotSupportedException($"Sided tests need to specify a side. {Test}");

            if (_side is Side.Client)
            {
                gt.Client.WaitAssertion(() =>
                    {
                        context.CurrentResult = innerCommand.Execute(context);
                    })
                    .Wait();
            }
            else
            {
                gt.Server.WaitAssertion(() =>
                    {
                        context.CurrentResult = innerCommand.Execute(context);
                    })
                    .Wait();
            }

            return context.CurrentResult;
        }
    }
}
