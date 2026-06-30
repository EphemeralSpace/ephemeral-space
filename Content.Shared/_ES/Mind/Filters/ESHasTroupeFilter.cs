using Content.Shared._ES.SecretIdentity;
using Content.Shared.Mind;
using Content.Shared.Mind.Filters;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Mind.Filters;

/// <summary>
/// Mind filter that excludes people who are not part of a particular troupe.
/// </summary>
[UsedImplicitly]
public sealed partial class ESHasTroupeFilter : MindFilter
{
    [DataField(required: true)]
    public ProtoId<ESTroupePrototype> Troupe;

    protected override bool ShouldRemove(Entity<MindComponent> mind, EntityUid? exclude, IEntityManager entMan, SharedMindSystem mindSys)
    {
        var maskSys = entMan.System<ESSharedSecretIdentitySystem>();
        return maskSys.GetTroupeOrNull(mind.AsNullable()) != Troupe;
    }
}
