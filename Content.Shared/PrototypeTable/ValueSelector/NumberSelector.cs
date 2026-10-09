using Content.Shared.PrototypeTable.PrototypeSelectors;
using JetBrains.Annotations;

namespace Content.Shared.PrototypeTable.ValueSelector;

/// <summary>
/// Used for implementing custom value selection for <see cref="PrototypeTableSelector{T}"/>
/// </summary>
[ImplicitDataDefinitionForInheritors, UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract partial class NumberSelector
{
    public abstract int Get(System.Random rand);
}
