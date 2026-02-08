using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Shared._ES.Auditions;
using Content.Shared._ES.Coroner;
using Content.Shared._ES.Masks;
using Content.Shared._ES.Masks.Components;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.ColorNaming;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._ES.Coroner;

public sealed class ESCoronerSystem : ESSharedCoronerSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ESCluesSystem _clues = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    protected override FormattedMessage GetReport(EntityUid target)
    {
        var msg = new FormattedMessage();
        if (!TryComp<HumanoidAppearanceComponent>(target, out var humanoidAppearance))
            return msg;

        var name = Name(target);
        var age = humanoidAppearance.Age;
        var sex = _clues.SexToString(humanoidAppearance.Sex);
        var eye = ColorNaming.Describe(humanoidAppearance.EyeColor, Loc);
        var hair = humanoidAppearance.MarkingSet.TryGetCategory(MarkingCategories.Hair, out var hairs)
                ? _clues.GetHairColorString(hairs.First().MarkingColors.First())
                : Loc.GetString("es-clue-hair-none");

        var timeOfDeath = _timing.CurTime;
        if (_mind.TryGetMind(target, out _, out var mind) && mind.TimeOfDeath.HasValue)
            timeOfDeath = mind.TimeOfDeath.Value;
        var time = (timeOfDeath - _gameTicker.RoundStartTimeSpan).ToString("hh\\:mm\\:ss");

        var mask = TryComp<ESBodyLastMaskComponent>(target, out var bodyLastMask)
            ? _prototype.Index(bodyLastMask.LastMask)
            : _random.Pick(_prototype.EnumeratePrototypes<ESMaskPrototype>().Where(p => !p.Abstract).ToList());

        msg.AddMarkupPermissive(Loc.GetString("es-coroner-report-paper",
            ("name", name),
            ("age", age),
            ("sex", sex),
            ("eye", eye),
            ("hair", hair),
            ("time", time),
            ("mask1", Loc.GetString(mask.Name))));
        return msg;
    }
}
