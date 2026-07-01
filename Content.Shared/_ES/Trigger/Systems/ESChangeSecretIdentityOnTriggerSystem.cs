using Content.Shared._ES.SecretIdentity;
using Content.Shared._ES.SecretIdentity.Components;
using Content.Shared._ES.Trigger.Component;
using Content.Shared.Mind;
using Content.Shared.Trigger;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Trigger.Systems;

public sealed partial class ESChangeSecretIdentityOnTriggerSystem : XOnTriggerSystem<ESChangeSecretIdentityOnTriggerComponent>
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private ESSharedSecretIdentitySystem _secretIdentity = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    protected override void OnTrigger(Entity<ESChangeSecretIdentityOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (args.User == null)
            return;

        if (!_mind.TryGetMind((EntityUid)args.User, out var mind))
            return;

        if (!ent.Comp.SameTroupeConversion)
        {
            if (!TryComp<ESBodyLastSecretIdentityComponent>(args.User, out var secretIdentity))
                return;

            var secretIdentityPrototype = _prototype.Index(ent.Comp.SecretIdentity);
            var lastSecretIdentityPrototype = _prototype.Index(secretIdentity.LastSecretIdentity);

            if (secretIdentityPrototype.Troupe == lastSecretIdentityPrototype.Troupe)
                return;
        }

        _secretIdentity.ChangeSecretIdentity(mind.Value, ent.Comp.SecretIdentity);
        args.Handled = true;
    }
}
