using Content.Shared._ES.Masks;
using Content.Shared._ES.Masks.Components;
using Content.Shared._ES.Trigger.Component;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared._ES.Trigger.Systems;

public sealed partial class ESChangeMaskOnTriggerSystem : XOnTriggerSystem<ESChangeMaskOnTriggerComponent>
{
    [Dependency] private ESSharedMaskSystem _mask = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    protected override void OnTrigger(Entity<ESChangeMaskOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (args.User == null)
            return;

        if (!_mind.TryGetMind((EntityUid)args.User, out var mind))
            return;

        if (!ent.Comp.SameMaskConversion)
        {
            if (!TryComp<ESBodyLastMaskComponent>(args.User, out var mask))
                return;

            if (mask.LastMask == ent.Comp.Mask)
                return;
        }

        _mask.ChangeMask(mind.Value, ent.Comp.Mask);
        args.Handled = true;
    }
}
