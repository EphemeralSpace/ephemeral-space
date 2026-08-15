using Content.Shared._ES.Chat;
using Content.Shared._ES.Chat.Radio;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;

namespace Content.Server.Implants;

public sealed partial class RadioImplantSystem : EntitySystem
{
    [Dependency] private ESSharedChatSystem _chat = default!;
    [Dependency] private ESRadioSystem _radio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadioImplantComponent, ImplantRelayEvent<ESGetRadioChannelsEvent>>(OnGetRadioChannels);
        SubscribeLocalEvent<RadioImplantComponent, ImplantRelayEvent<ESGetChatPermissionsEvent>>(OnGetChatPermissions);
        SubscribeLocalEvent<RadioImplantComponent, ImplantImplantedEvent>(OnImplantImplanted);
        SubscribeLocalEvent<RadioImplantComponent, ImplantRemovedEvent>(OnImplantRemoved);
    }

    private void OnGetRadioChannels(Entity<RadioImplantComponent> ent, ref ImplantRelayEvent<ESGetRadioChannelsEvent> args)
    {
        foreach (var channel in ent.Comp.RadioChannels)
        {
            args.Event.Channels.Add(channel);
        }
    }

    private void OnGetChatPermissions(Entity<RadioImplantComponent> ent, ref ImplantRelayEvent<ESGetChatPermissionsEvent> args)
    {
        foreach (var channel in ent.Comp.RadioChannels)
        {
            args.Event.Channels.Add(channel);
        }
    }

    /// <summary>
    /// If implanted with a radio implant, installs the necessary intrinsic radio components
    /// </summary>
    private void OnImplantImplanted(Entity<RadioImplantComponent> ent, ref ImplantImplantedEvent args)
    {
        _radio.RefreshRadioChannels(args.Implanted);
        _chat.RefreshChatPermissions(args.Implanted);
    }

    /// <summary>
    /// Removes intrinsic radio components once the Radio Implant is removed
    /// </summary>
    private void OnImplantRemoved(Entity<RadioImplantComponent> ent, ref ImplantRemovedEvent args)
    {
        _radio.RefreshRadioChannels(args.Implanted);
        _chat.RefreshChatPermissions(args.Implanted);
    }
}
