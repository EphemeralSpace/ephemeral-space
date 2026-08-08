using Content.Shared._ES.SecretIdentity;
using Content.Shared._ES.Trigger.Components;
using Content.Shared.Mind;
using Content.Shared.Trigger;

namespace Content.Shared._ES.Trigger.Systems;

public sealed partial class ESChangeSecretIdentityOnTriggerSystem : XOnTriggerSystem<ESChangeSecretIdentityOnTriggerComponent>
{
    [Dependency] private ESSharedSecretIdentitySystem _secretIdentity = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    protected override void OnTrigger(Entity<ESChangeSecretIdentityOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (args.User == null)
            return;

        if (!_mind.TryGetMind((EntityUid)args.User, out var mind))
            return;

        if (!ent.Comp.SameOrganizationConversion)
        {
            if (_secretIdentity.GetSecretIdentityOrNull(mind.Value.AsNullable()) == ent.Comp.SecretIdentity)
                return;
        }

        _secretIdentity.ChangeSecretIdentity(mind.Value, ent.Comp.SecretIdentity);
        args.Handled = true;
    }
}
