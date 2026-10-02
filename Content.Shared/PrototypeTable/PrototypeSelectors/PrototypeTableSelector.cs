using Content.Shared.PrototypeTable.Conditions;
using Content.Shared.PrototypeTable.ValueSelector;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.PrototypeTable.PrototypeSelectors;

// no datadef stuff since we uhh kind of have to serialize shit manually with PrototypeTableTypeSerializer
// to get around datadefs not supporting generics very well
[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
[CopyByRef]
public abstract class PrototypeTableSelector<T>
    where T: class, IPrototype
{
    /// <summary>
    /// The number of times this selector is run
    /// </summary>
    public NumberSelector Rolls = new ConstantNumberSelector(1);

    /// <summary>
    /// A weight used to pick between selectors.
    /// </summary>
    public float Weight = 1;

    /// <summary>
    /// A simple chance that the selector will run.
    /// </summary>
    public double Prob = 1;

    /// <summary>
    /// A list of conditions that must evaluate to 'true' for the selector to apply.
    /// </summary>
    public List<PrototypeTableCondition> Conditions = new();

    /// <summary>
    /// If true, all the conditions must be successful in order for the selector to process.
    /// Otherwise, only one of them must be.
    /// </summary>
    public bool RequireAll = true;

    public IEnumerable<ProtoId<T>> GetSpawns(System.Random rand,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        if (!CheckConditions(entMan, proto, ctx))
            yield break;

        var rolls = Rolls.Get(rand);
        for (var i = 0; i < rolls; i++)
        {
            if (!rand.Prob(Prob))
                continue;

            foreach (var spawn in GetSpawnsImplementation(rand, entMan, proto, ctx))
            {
                yield return spawn;
            }
        }
    }

    public bool CheckConditions(IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    {
        if (Conditions.Count == 0)
            return true;

        var success = false;
        foreach (var condition in Conditions)
        {
            var res = condition.Evaluate(this, entMan, proto, ctx);

            if (RequireAll && !res)
                return false; // intentional break out of loop and function

            success |= res;
        }

        if (RequireAll)
            return true;

        return success;
    }

    protected abstract IEnumerable<ProtoId<T>> GetSpawnsImplementation(System.Random rand,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx);
}
