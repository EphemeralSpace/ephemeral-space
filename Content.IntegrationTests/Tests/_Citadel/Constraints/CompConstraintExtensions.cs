#nullable enable
using Content.IntegrationTests.Tests._Citadel.Operators;
using NUnit.Framework.Constraints;
using Robust.Shared.GameObjects;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests._Citadel.Constraints;

public static class CompConstraintExtensions
{
    extension(Has)
    {
        public static ResolvableConstraintExpression Comp<T>(IIntegrationInstance instance)
            where T : IComponent
        {
            return new ConstraintExpression().Comp<T>(instance);
        }

        public static ResolvableConstraintExpression Comp(Type t, IIntegrationInstance instance)
        {
            return new ConstraintExpression().Comp(t, instance);
        }
    }

    extension(ConstraintExpression expr)
    {
        public ResolvableConstraintExpression Comp<T>(IIntegrationInstance instance)
            where T : IComponent
        {
            return expr.Append(new CompOperator(typeof(T), instance));
        }

        public ResolvableConstraintExpression Comp(Type t, IIntegrationInstance instance)
        {
            return expr.Append(new CompOperator(t, instance));
        }
    }
}
