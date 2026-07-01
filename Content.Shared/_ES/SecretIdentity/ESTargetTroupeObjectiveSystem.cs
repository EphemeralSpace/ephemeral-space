using Content.Shared._ES.SecretIdentity.Components;
using Content.Shared._ES.Objectives.Target.Components;

namespace Content.Shared._ES.SecretIdentity;

public sealed partial class ESTargetTroupeObjectiveSystem : EntitySystem
{
    [Dependency] private ESSharedSecretIdentitySystem _secretIdentity = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESTargetTroupeObjectiveComponent, ESValidateObjectiveTargetCandidates>(OnValidateTarget);
    }

    private void OnValidateTarget(Entity<ESTargetTroupeObjectiveComponent> ent, ref ESValidateObjectiveTargetCandidates args)
    {
        // its kind of weird for this logic to be here but its weird to even need this logic and sympathizer is weird so.
        if (_secretIdentity.GetSecretIdentityOrNull(args.Candidate) is { } secretIdentity && ent.Comp.OverrideSecretIdentities.Contains(secretIdentity))
            return;

        if ((_secretIdentity.GetTroupeOrNull(args.Candidate) != ent.Comp.Troupe) ^ ent.Comp.Invert)
            args.Invalidate();
    }
}
