using Content.Shared._ES.Chat.Radio;
using Content.Shared._Offbrand.StatusEffects;
using Content.Shared.Emp;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Radio.Components;
using Content.Shared.StatusEffectNew;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Shared.Radio.EntitySystems;

public sealed partial class HeadsetSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ESRadioSystem _radio = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!; // Offbrand

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeadsetComponent, EncryptionChannelsChangedEvent>(OnKeysChanged);
        SubscribeLocalEvent<HeadsetComponent, InventoryRelayedEvent<GetDefaultRadioChannelEvent>>(OnGetDefault);
        SubscribeLocalEvent<HeadsetComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<HeadsetComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<HeadsetComponent, EmpPulseEvent>(OnEmpPulse);

        SubscribeLocalEvent<WearingHeadsetComponent, ESGetRadioChannelsEvent>(OnGetRadioChannels);
    }

    [PublicAPI]
    public void SetEnabled(Entity<HeadsetComponent?> ent, bool value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Enabled == value)
            return;

        ent.Comp.Enabled = value;
        Dirty(ent);

        if (ent.Comp.IsEquipped)
            _radio.RefreshRadioChannels(Transform(ent).ParentUid);
    }

    private void OnKeysChanged(EntityUid uid, HeadsetComponent component, EncryptionChannelsChangedEvent args)
    {
        if (component.IsEquipped)
            _radio.RefreshRadioChannels(Transform(uid).ParentUid);
    }

    private void OnGetDefault(EntityUid uid, HeadsetComponent component, InventoryRelayedEvent<GetDefaultRadioChannelEvent> args)
    {
        if (!component.Enabled || !component.IsEquipped)
        {
            // don't provide default channels from pocket slots.
            return;
        }

        if (TryComp(uid, out EncryptionKeyHolderComponent? keyHolder))
            args.Args.Channel ??= keyHolder.DefaultChannel;
    }

    private void OnGotEquipped(EntityUid uid, HeadsetComponent component, GotEquippedEvent args)
    {
        component.IsEquipped = args.SlotFlags.HasFlag(component.RequiredSlot);
        Dirty(uid, component);

        if (!_timing.ApplyingState && component.IsEquipped)
        {
            EnsureComp<WearingHeadsetComponent>(args.Equipee).Headset = uid;
            _radio.RefreshRadioChannels(args.Equipee);
        }
    }

    private void OnGotUnequipped(EntityUid uid, HeadsetComponent component, GotUnequippedEvent args)
    {
        component.IsEquipped = false;
        Dirty(uid, component);

        if (!_timing.ApplyingState)
        {
            RemComp<WearingHeadsetComponent>(args.Equipee);
            _radio.RefreshRadioChannels(args.Equipee);
        }
    }

    private void OnEmpPulse(Entity<HeadsetComponent> ent, ref EmpPulseEvent args)
    {
        if (ent.Comp.Enabled)
        {
            args.Affected = true;
            args.Disabled = true;
        }
    }

    private void OnGetRadioChannels(Entity<WearingHeadsetComponent> ent, ref ESGetRadioChannelsEvent args)
    {
        if (!TryComp<HeadsetComponent>(ent.Comp.Headset, out var headset) ||
            !TryComp<EncryptionKeyHolderComponent>(ent.Comp.Headset, out var holder))
            return;

        if (_statusEffects.HasEffectComp<CannotUseHeadsetStatusEffectComponent>(ent))
            return;

        if (!headset.Enabled)
            return;

        foreach (var channel in holder.Channels)
        {
            args.Channels.Add(channel);
        }
    }
}
