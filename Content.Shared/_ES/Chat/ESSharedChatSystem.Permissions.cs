using Content.Shared._ES.Chat.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chat;

public abstract partial class ESSharedChatSystem
{
    private void InitializePermissions()
    {
        SubscribeLocalEvent<ESChatPermissionsComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ESChatPermissionsComponent, ESGetChatPermissionsEvent>(OnGetChatPermissions);
    }

    private void OnStartup(Entity<ESChatPermissionsComponent> ent, ref ComponentStartup args)
    {
        RefreshChatPermissions(ent.AsNullable());
    }

    private void OnGetChatPermissions(Entity<ESChatPermissionsComponent> ent, ref ESGetChatPermissionsEvent args)
    {
        foreach (var channel in ent.Comp.InherentChannels)
        {
            args.Channels.Add(channel);
        }
    }

    public virtual void RefreshChatPermissions(Entity<ESChatPermissionsComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var ev = new ESGetChatPermissionsEvent(ent);
        RaiseLocalEvent(ent, ref ev, true);

        ent.Comp.PermittedChannels = ev.Channels;
        Dirty(ent);
    }

    public HashSet<ProtoId<ESChatChannelPrototype>> GetPermittedChannels(Entity<ESChatPermissionsComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return [ DefaultChannel ];

        return ent.Comp.PermittedChannels;
    }
}

/// <summary>
/// Event broadcast and raised on an entity to determine what chat channels they can send from.
/// </summary>
[ByRefEvent]
public record struct ESGetChatPermissionsEvent(EntityUid Source)
{
    public readonly EntityUid Source = Source;

    public readonly HashSet<ProtoId<ESChatChannelPrototype>> Channels = [];
}

