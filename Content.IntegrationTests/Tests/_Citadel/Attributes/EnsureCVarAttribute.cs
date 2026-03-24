using System.Reflection;
using Robust.Shared.Configuration;

namespace Content.IntegrationTests.Tests._Citadel.Attributes;

/// <summary>
///     Ensures the given CVar, on the given side (or both), is the given value.
///     Attribute version of <see cref="GameTest.OverrideCVar"/>, and stores the old value the same way.
/// </summary>
/// <remarks>This only works with <see cref="GameTest"/> fixtures.</remarks>
/// <param name="side">The side to set the CVar on, or both.</param>
/// <param name="definitionType">The type the CVar is defined on.</param>
/// <param name="fieldName">The name of the static field defining the CVar.</param>
/// <param name="value">The value to set the CVar to.</param>
/// <example>
/// <code>
///     [Test]
///     [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.FlavorText), true)]
///     public async Task MyTest()
///     {
///         // CVar is set for you inside the test, and automatically un-set on teardown.
///     }
/// </code>
/// </example>
/// <seealso cref="GameTest"/>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class EnsureCVarAttribute(Side side, Type definitionType, string fieldName, object value) : Attribute, IGameTestModifier
{
    Task IGameTestModifier.ApplyToTest(GameTest test)
    {
        var field = definitionType.GetField(fieldName, BindingFlags.Static | BindingFlags.Public);
        var cvar = (CVarDef)field!.GetValue(field);

        // cvars cannot be null
        if (value.GetType() != cvar!.DefaultValue.GetType())
            throw new NotSupportedException($"Cannot set {cvar.Name} to {value}, it's the wrong type.");

        test.PreTestAddOverride(side, cvar!.Name, value);

        return Task.CompletedTask;
    }
}
