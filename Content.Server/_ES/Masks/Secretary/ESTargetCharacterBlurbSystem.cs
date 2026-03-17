using Content.Server._ES.Masks.Secretary.Components;
using Content.Shared._ES.Objectives.Target.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.Masks.Secretary;

public sealed class ESTargetCharacterBlurbSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESTargetCharacterBlurbComponent, ESObjectiveTargetChangedEvent>(OnTargetChanged);
    }

    private void OnTargetChanged(Entity<ESTargetCharacterBlurbComponent> ent, ref ESObjectiveTargetChangedEvent args)
    {
        // Only generate when we get a new target.
        if (!args.NewTarget.HasValue)
            return;

        var formatDataset = _prototype.Index(ent.Comp.TargetFormatDataset);
        var contextDataset = _prototype.Index(ent.Comp.ContextDataset);

        var name = Name(args.NewTarget.Value);
        var format = _random.Pick(formatDataset);
        var context = _random.Pick(contextDataset);

        ent.Comp.Blurb = Loc.GetString(context, ("target", Loc.GetString(format, ("target", name))));
    }
}
