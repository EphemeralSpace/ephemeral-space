using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry;

[UsedImplicitly]
public sealed partial class ReactiveSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;

    public void DoEntityReaction(EntityUid uid, Solution solution, ReactionMethod method, EntityUid? origin = null)
    {
        foreach (var reagent in solution.Contents.ToArray())
        {
            ReactionEntity(uid, method, reagent, origin);
        }
    }

    public void ReactionEntity(EntityUid uid, ReactionMethod method, ReagentQuantity reagentQuantity, EntityUid? origin = null)
    {
        if (reagentQuantity.Quantity == FixedPoint2.Zero)
            return;

        // We throw if the reagent specified doesn't exist.
        if (!_proto.Resolve<ReagentPrototype>(reagentQuantity.Reagent.Prototype, out var proto))
            return;

        var ev = new ReactionEntityEvent(method, reagentQuantity, proto, origin);
        RaiseLocalEvent(uid, ref ev);
    }
}
public enum ReactionMethod
{
Touch,
Injection,
Ingestion,
}

[ByRefEvent]
public readonly record struct ReactionEntityEvent(ReactionMethod Method, ReagentQuantity ReagentQuantity, ReagentPrototype Reagent, EntityUid? Origin);
