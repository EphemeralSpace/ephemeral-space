using Content.Shared._Citadel.Utilities;
using Content.Shared._ES.Degradation.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._ES.Degradation;

public sealed class ESAirlockFailureSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <summary>
    /// Adds an airlock failure charge to the target entity.
    /// </summary>
    public void AddFailureCharge(EntityUid target)
    {
        var comp = EnsureComp<ESAirlockFailerComponent>(target);
        ++comp.Charges;
        Dirty(target, comp);
    }

    /// <summary>
    /// Consumes an airlock failure charge, decrementing it and cleaning up the component if necessary.
    /// </summary>
    public bool TryUseFailureCharge(Entity<ESAirlockFailerComponent?> target)
    {
        if (!Resolve(target, ref target.Comp, false))
            return false;

        var seed = new RngSeed().SeedForStep((int) _timing.CurTick.Value + target.Owner.Id);
        var random = seed.IntoRandomizer();

        if (!random.Prob(target.Comp.FailChance))
            return false;

        --target.Comp.Charges;
        Dirty(target);

        if (target.Comp.Charges == 0)
        {
            RemCompDeferred(target, target.Comp);
        }
        return true;
    }
}
