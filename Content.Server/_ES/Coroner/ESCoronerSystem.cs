using System.Linq;
using Content.Server._ES.Masks.Masquerades;
using Content.Shared._ES.Auditions;
using Content.Shared._ES.Coroner;
using Content.Shared._ES.Masks;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._ES.Coroner;

public sealed class ESCoronerSystem : ESSharedCoronerSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ESCluesSystem _clues = default!;
    [Dependency] private readonly ESSharedMaskSystem _mask = default!;

    protected override FormattedMessage GetReport(EntityUid target)
    {
        var msg = new FormattedMessage();
        if (!TryComp<HumanoidAppearanceComponent>(target, out var humanoidAppearance))
            return msg;

        var name = Name(target);
        var age = humanoidAppearance.Age;
        var sex = _clues.SexToString(humanoidAppearance.Sex);

        var allMasks = _prototype.EnumeratePrototypes<ESMaskPrototype>()
            .Where(p => !p.Abstract)
            .ToList();

        // The mask of our player, or just a random mask
        var realMask = _mask.GetMaskOrNull(target) ?? _random.Pick(allMasks);

        var validMasks = EntityQuery<ESMasqueradeRuleComponent>().SingleOrDefault()?.AssignedMasks
                    ?? allMasks.Select(p => new ProtoId<ESMaskPrototype>(p.ID)).ToList();

        var fakeMask = _random.Pick(validMasks.Where(m => m != realMask).ToList());

        // Which one is real? which one is fake? who will ever know...
        if (_random.Prob(0.5f))
        {
            (realMask, fakeMask) = (fakeMask, realMask);
        }

        var mask1 = Loc.GetString(_prototype.Index(realMask).Name);
        var mask2 = Loc.GetString(_prototype.Index(fakeMask).Name);

        msg.AddMarkupPermissive(Loc.GetString("es-coroner-report-paper",
            ("name", name),
            ("age", age),
            ("sex", sex),
            ("mask1", mask1),
            ("mask2", mask2)));
        return msg;
    }
}
