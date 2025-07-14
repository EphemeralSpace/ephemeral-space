using Content.Shared._ES.Keypad.Components;
using Content.Shared.Construction;
using Content.Shared.Examine;
using JetBrains.Annotations;

namespace Content.Shared._ES.Construction.Conditions;

[UsedImplicitly]
public sealed partial class ESKeypadUnlockedCondition : IGraphCondition
{
    public bool Condition(EntityUid uid, IEntityManager entityManager)
    {
        return !entityManager.TryGetComponent<ESKeypadComponent>(uid, out var component) || !component.Locked;
    }

    public bool DoExamine(ExaminedEvent args)
    {
        if (Condition(args.Examined, IoCManager.Resolve<IEntityManager>()))
            return false;

        args.PushMarkup(Loc.GetString("es-construction-examine-condition-keypad-unlocked"));
        return true;
    }

    public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        yield return new ConstructionGuideEntry()
        {
            Localization = "es-construction-examine-condition-keypad-unlocked",
        };
    }
}
