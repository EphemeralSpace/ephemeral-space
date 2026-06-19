using Content.Shared._ES.Masks;
using Content.Shared._ES.Masks.Components;
using Content.Shared._ES.Trigger.Component;
using Content.Shared.Mind;
using Content.Shared.Trigger;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Trigger.Systems;

public sealed partial class ESChangeMaskOnTriggerSystem : XOnTriggerSystem<ESChangeMaskOnTriggerComponent>
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private ESSharedMaskSystem _mask = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    protected override void OnTrigger(Entity<ESChangeMaskOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (args.User == null)
            return;

        if (!_mind.TryGetMind((EntityUid)args.User, out var mind))
            return;

        if (!ent.Comp.SameTroupeConversion)
        {
            if (!TryComp<ESBodyLastMaskComponent>(args.User, out var mask))
                return;

            var maskPrototype = _prototype.Index(ent.Comp.Mask);
            var lastMaskPrototype = _prototype.Index(mask.LastMask);

            if (maskPrototype.Troupe == lastMaskPrototype.Troupe)
                return;
        }

        _mask.ChangeMask(mind.Value, ent.Comp.Mask);
        args.Handled = true;
    }
}
