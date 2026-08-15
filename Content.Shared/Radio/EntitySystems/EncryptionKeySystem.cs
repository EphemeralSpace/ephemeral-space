using System.Linq;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Radio.Components;
using Content.Shared.Tools.Components;
using Content.Shared.Wires;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using SharedToolSystem = Content.Shared.Tools.Systems.SharedToolSystem;

namespace Content.Shared.Radio.EntitySystems;

/// <summary>
///     This system manages encryption keys & key holders for use with radio channels.
/// </summary>
public sealed partial class EncryptionKeySystem : EntitySystem
{
    [Dependency] private SharedToolSystem _tool = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedWiresSystem _wires = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EncryptionKeyHolderComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<EncryptionKeyHolderComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<EncryptionKeyHolderComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<EncryptionKeyHolderComponent, EntRemovedFromContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<EncryptionKeyHolderComponent, EncryptionRemovalFinishedEvent>(OnKeyRemoval);
    }

    private void OnKeyRemoval(EntityUid uid, EncryptionKeyHolderComponent component, EncryptionRemovalFinishedEvent args)
    {
        if (args.Cancelled)
            return;

        var contained = component.KeyContainer.ContainedEntities.ToArray();
        _container.EmptyContainer(component.KeyContainer, reparent: false);
        foreach (var ent in contained)
        {
            _hands.PickupOrDrop(args.User, ent, dropNear: true);
        }

        _popup.PopupEntity(Loc.GetString("encryption-keys-all-extracted"), uid, args.User);
        _audio.PlayPredicted(component.KeyExtractionSound, uid, args.User);
    }

    public void UpdateChannels(EntityUid uid, EncryptionKeyHolderComponent component)
    {
        if (!component.Initialized)
            return;

        component.Channels.Clear();
        component.DefaultChannel = null;

        foreach (var ent in component.KeyContainer.ContainedEntities)
        {
            if (TryComp<EncryptionKeyComponent>(ent, out var key))
            {
                component.Channels.UnionWith(key.Channels);
                component.DefaultChannel ??= key.DefaultChannel;
            }
        }

        RaiseLocalEvent(uid, new EncryptionChannelsChangedEvent(component));
    }

    private void OnContainerModified(EntityUid uid, EncryptionKeyHolderComponent component, ContainerModifiedMessage args)
    {
        if (args.Container.ID == EncryptionKeyHolderComponent.KeyContainerName)
            UpdateChannels(uid, component);
    }

    private void OnInteractUsing(EntityUid uid, EncryptionKeyHolderComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<EncryptionKeyComponent>(args.Used))
        {
            args.Handled = true;
            TryInsertKey(uid, component, args);
        }
        else if (TryComp<ToolComponent>(args.Used, out var tool)
                 && _tool.HasQuality(args.Used, component.KeysExtractionMethod, tool)
                 && component.KeyContainer.ContainedEntities.Count > 0) // dont block deconstruction
        {
            args.Handled = true;
            TryRemoveKey(uid, component, args, tool);
        }
    }

    private void TryInsertKey(EntityUid uid, EncryptionKeyHolderComponent component, InteractUsingEvent args)
    {
        if (!component.KeysUnlocked)
        {
            _popup.PopupEntity(Loc.GetString("encryption-keys-are-locked"), uid, args.User);
            return;
        }

        if (TryComp<WiresPanelComponent>(uid, out var panel) && !panel.Open)
        {
            _popup.PopupEntity(Loc.GetString("encryption-keys-panel-locked"), uid, args.User);
            return;
        }

        if (component.KeySlots <= component.KeyContainer.ContainedEntities.Count)
        {
            _popup.PopupEntity(Loc.GetString("encryption-key-slots-already-full"), uid, args.User);
            return;
        }

        if (_container.Insert(args.Used, component.KeyContainer))
        {
            _popup.PopupEntity(Loc.GetString("encryption-key-successfully-installed"), uid, args.User);
            _audio.PlayPredicted(component.KeyInsertionSound, args.Target, args.User);
            args.Handled = true;
            return;
        }
    }

    private void TryRemoveKey(EntityUid uid, EncryptionKeyHolderComponent component, InteractUsingEvent args,
        ToolComponent? tool)
    {
        if (!component.KeysUnlocked)
        {
            _popup.PopupEntity(Loc.GetString("encryption-keys-are-locked"), uid, args.User);
            return;
        }

        if (!_wires.IsPanelOpen(uid))
        {
            _popup.PopupEntity(Loc.GetString("encryption-keys-panel-locked"), uid, args.User);
            return;
        }

        if (component.KeyContainer.ContainedEntities.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("encryption-keys-no-keys"), uid, args.User);
            return;
        }

        _tool.UseTool(args.Used, args.User, uid, 1f, component.KeysExtractionMethod, new EncryptionRemovalFinishedEvent(), toolComponent: tool);
    }

    private void OnStartup(EntityUid uid, EncryptionKeyHolderComponent component, ComponentStartup args)
    {
        component.KeyContainer = _container.EnsureContainer<Container>(uid, EncryptionKeyHolderComponent.KeyContainerName);
        UpdateChannels(uid, component);
    }

    [Serializable, NetSerializable]
    public sealed partial class EncryptionRemovalFinishedEvent : SimpleDoAfterEvent
    {
    }
}
