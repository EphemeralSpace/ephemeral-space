using Content.Server._ES.SecretIdentity.Hemophage.Components;
using Content.Server.Mind;
using Robust.Shared.Physics.Events;

namespace Content.Server._ES.SecretIdentity.Hemophage;

public sealed partial class ESSecretIdentityConvertOnCollideSystem : EntitySystem
{
    [Dependency] private ESSecretIdentitySystem _secretIdentity = default!;
    [Dependency] private MindSystem _mind = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESSecretIdentityConvertOnCollideComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(Entity<ESSecretIdentityConvertOnCollideComponent> ent, ref StartCollideEvent args)
    {
        if (!_mind.TryGetMind(args.OtherEntity, out var mind))
            return;

        if (_secretIdentity.GetOrganizationOrNull(args.OtherEntity) == ent.Comp.IgnoreOrganization)
            return;

        _secretIdentity.ChangeSecretIdentity(mind.Value, ent.Comp.SecretIdentity);
    }
}
