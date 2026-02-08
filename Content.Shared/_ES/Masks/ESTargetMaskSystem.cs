using Content.Shared._ES.Masks.Components;
using Content.Shared._ES.Objectives.Target.Components;

namespace Content.Shared._ES.Masks;

public sealed class ESTargetMaskSystem : EntitySystem
{
    [Dependency] private readonly ESSharedMaskSystem _mask = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESTargetMaskBlacklistComponent, ESValidateObjectiveTargetCandidates>(Handler);
    }

    private void Handler(Entity<ESTargetMaskBlacklistComponent> ent, ref ESValidateObjectiveTargetCandidates args)
    {
        if (_mask.GetMaskOrNull(args.Candidate) is {} mask && ent.Comp.MaskBlacklist.Contains(mask))
            args.Invalidate();
    }
}
