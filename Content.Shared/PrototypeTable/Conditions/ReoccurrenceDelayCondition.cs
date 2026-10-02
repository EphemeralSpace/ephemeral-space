using System.Linq;
using Content.Shared.PrototypeTable.PrototypeSelectors;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;

namespace Content.Shared.PrototypeTable.Conditions;

public sealed partial class ReoccurrenceDelayCondition : PrototypeTableCondition
{
    /// <summary>
    /// The maximum amount of times this rule can have already be run.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.Zero;

    /// <summary>
    /// The rule that is being checked for occurrences.
    /// If null, it will use the value on the attached selector.
    /// </summary>
    [DataField]
    public EntProtoId? RuleOverride;

    protected override bool EvaluateImplementation<T>(PrototypeTableSelector<T> root, IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    {
        string rule;
        if (RuleOverride is { } ruleOverride)
        {
            rule = ruleOverride;
        }
        else
        {
            rule = root is IdSelector<T> entSelector
                ? entSelector.Id
                : string.Empty;
        }

        if (rule == string.Empty)
            return false;

        var gameTicker = entMan.System<SharedGameTicker>();

        return gameTicker.AllPreviousGameRules.Any(p => p.Item2 == rule && p.Item1 + Delay <= gameTicker.RoundDuration());
    }
}
