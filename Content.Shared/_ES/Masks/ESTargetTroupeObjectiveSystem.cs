using Content.Shared._ES.Masks.Components;
using Content.Shared._ES.Objectives.Target.Components;

namespace Content.Shared._ES.Masks;

public sealed class ESTargetTroupeObjectiveSystem : EntitySystem
{
    [Dependency] private ESSharedMaskSystem _mask = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESTargetTroupeObjectiveComponent, ESValidateObjectiveTargetCandidates>(OnValidateTarget);
    }

    private void OnValidateTarget(Entity<ESTargetTroupeObjectiveComponent> ent, ref ESValidateObjectiveTargetCandidates args)
    {
        if ((_mask.GetTroupeOrNull(args.Candidate) != ent.Comp.Troupe) ^ ent.Comp.Invert)
            args.Invalidate();
    }
}
