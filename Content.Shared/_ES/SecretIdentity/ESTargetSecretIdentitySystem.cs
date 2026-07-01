using Content.Shared._ES.SecretIdentity.Components;
using Content.Shared._ES.Objectives.Target.Components;

namespace Content.Shared._ES.SecretIdentity;

public sealed partial class ESTargetSecretIdentitySystem : EntitySystem
{
    [Dependency] private ESSharedSecretIdentitySystem _secretIdentity = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESTargetSecretIdentityBlacklistComponent, ESValidateObjectiveTargetCandidates>(Handler);
    }

    private void Handler(Entity<ESTargetSecretIdentityBlacklistComponent> ent, ref ESValidateObjectiveTargetCandidates args)
    {
        if (_secretIdentity.GetSecretIdentityOrNull(args.Candidate) is {} secretIdentity && ent.Comp.SecretIdentityBlacklist.Contains(secretIdentity))
            args.Invalidate();
    }
}
