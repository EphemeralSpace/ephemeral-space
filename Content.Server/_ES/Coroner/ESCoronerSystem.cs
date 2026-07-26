using System.Linq;
using Content.Server._ES.SecretIdentity;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Shared._ES.Auditions;
using Content.Shared._ES.Coroner;
using Content.Shared._ES.SecretIdentity;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.ColorNaming;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._ES.Coroner;

public sealed partial class ESCoronerSystem : ESSharedCoronerSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ESCluesSystem _clues = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private ESSecretIdentitySystem _secretIdentity = default!;

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

        var secretIdentity = _random.Pick(_prototype.EnumeratePrototypes<ESSecretIdentityPrototype>()
            .Where(p => !p.Abstract)
            .ToList());
        if (_secretIdentity.TryGetLastSecretIdentity(target, out var bodyLastSecretIDentity))
            secretIdentity = _prototype.Index(bodyLastSecretIDentity.Value);

        msg.AddMarkupPermissive(Loc.GetString("es-coroner-report-paper",
            ("name", name),
            ("age", age),
            ("sex", sex),
            ("eye", eye),
            ("hair", hair),
            ("time", time),
            ("secretIdentity1", Loc.GetString(secretIdentity.Name))));
        return msg;
    }
}
