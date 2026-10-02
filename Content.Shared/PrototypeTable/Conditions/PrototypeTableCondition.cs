using Content.Shared.PrototypeTable.PrototypeSelectors;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.PrototypeTable.Conditions;

/// <summary>
/// Used for implementing conditional logic for <see cref="PrototypeTableSelector{T}"/>.
/// </summary>
[ImplicitDataDefinitionForInheritors, UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract partial class PrototypeTableCondition
{
    /// <summary>
    /// If true, inverts the result of the condition.
    /// </summary>
    [DataField]
    public bool Invert;

    public bool Evaluate<T>(PrototypeTableSelector<T> root, IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
        where T: class, IPrototype
    {
        var res = EvaluateImplementation(root, entMan, proto, ctx);

        // XOR eval to invert the result.
        return res ^ Invert;
    }

    protected abstract bool EvaluateImplementation<T>(PrototypeTableSelector<T> root, IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx) where T : class, IPrototype;
}
