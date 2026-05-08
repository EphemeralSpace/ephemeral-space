using Content.Shared._ES.Masks.Components;
using Content.Shared._ES.Objectives.Target.Components;

namespace Content.Shared._ES.Masks;

public sealed partial class ESTargetTroupeObjectiveSystem : EntitySystem
{
    [Dependency] private ESSharedMaskSystem _mask = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESTargetTroupeObjectiveComponent, ESValidateObjectiveTargetCandidates>(OnValidateTarget);
    }

    private void OnValidateTarget(Entity<ESTargetTroupeObjectiveComponent> ent, ref ESValidateObjectiveTargetCandidates args)
    {
        // its kind of weird for this logic to be here but its weird to even need this logic and sympathizer is weird so.
        if (_mask.GetMaskOrNull(args.Candidate) is { } mask && ent.Comp.OverrideMasks.Contains(mask))
            return;

        if ((_mask.GetTroupeOrNull(args.Candidate) != ent.Comp.Troupe) ^ ent.Comp.Invert)
            args.Invalidate();
    }
}
