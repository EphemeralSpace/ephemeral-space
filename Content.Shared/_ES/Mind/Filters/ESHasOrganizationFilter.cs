using Content.Shared._ES.SecretIdentity;
using Content.Shared.Mind;
using Content.Shared.Mind.Filters;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Mind.Filters;

/// <summary>
/// Mind filter that excludes people who are not part of a particular organization.
/// </summary>
[UsedImplicitly]
public sealed partial class ESHasOrganizationFilter : MindFilter
{
    [DataField(required: true)]
    public ProtoId<ESOrganizationPrototype> Organization;

    protected override bool ShouldRemove(Entity<MindComponent> mind, EntityUid? exclude, IEntityManager entMan, SharedMindSystem mindSys)
    {
        var secretIdentitySys = entMan.System<ESSharedSecretIdentitySystem>();
        return secretIdentitySys.GetOrganizationOrNull(mind.AsNullable()) != Organization;
    }
}
