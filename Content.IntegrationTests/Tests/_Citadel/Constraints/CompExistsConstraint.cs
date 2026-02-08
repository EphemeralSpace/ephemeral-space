#nullable enable
using NUnit.Framework.Constraints;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests._Citadel.Constraints;

/// <summary>
///     Constraint for whether a component exists.
/// </summary>
/// <seealso cref="CompConstraintExtensions"/>
public sealed class CompExistsConstraint(Type component, IIntegrationInstance instance) : Constraint
{
    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        if (!ConstraintHelpers.TryActualAsEnt(actual, out var ent, out var error))
        {
            if (error)
            {
                throw new NotImplementedException(
                    $"The input type {typeof(TActual)} to {nameof(CompExistsConstraint)} is not a supported entity id.");
            }

            return new ConstraintResult(this, actual, ConstraintStatus.Failure);
        }

        return new ConstraintResult(this, actual, instance.EntMan.HasComponent(ent, component));
    }

    public override string Description => $"has the component {component.Name}";
}
