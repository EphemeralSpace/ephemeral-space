using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._ES.Tips;

/// <summary>
///     Exposes picking a random tip from our tip prototypes & localizing tip text properly.
/// </summary>
[PublicAPI]
public sealed class ESTipsManager
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;

    /// <summary>
    ///     Returns already-localized tip text for a random non-hidden tip.
    /// </summary>
    public string GetRandomTip()
    {
        var applicableTips = _proto.EnumeratePrototypes<ESTipPrototype>().Where(p => !p.Hidden).ToArray();
        return GetTipText(_random.Pick(applicableTips));
    }

    /// <summary>
    ///     Returns properly localized (if a loc id is available) tip text for a tip.
    /// </summary>
    public string GetTipText(ESTipPrototype tip)
    {
        return _loc.TryGetString($"es-tip-{tip.ID}", out var str) ? str : tip.UnlocalizedText;
    }
}
