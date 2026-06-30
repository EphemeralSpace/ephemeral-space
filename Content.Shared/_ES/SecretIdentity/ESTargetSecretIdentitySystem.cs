using Content.Shared._ES.SecretIdentity.Components;
using Content.Shared._ES.Objectives.Target.Components;

namespace Content.Shared._ES.SecretIdentity;

public sealed partial class ESTargetMaskSystem : EntitySystem
{
    [Dependency] private ESSharedSecretIdentitySystem _secretIdentity = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESTargetMaskBlacklistComponent, ESValidateObjectiveTargetCandidates>(Handler);
    }

    private void Handler(Entity<ESTargetMaskBlacklistComponent> ent, ref ESValidateObjectiveTargetCandidates args)
    {
        if (_secretIdentity.GetMaskOrNull(args.Candidate) is {} mask && ent.Comp.MaskBlacklist.Contains(mask))
            args.Invalidate();
    }
}
