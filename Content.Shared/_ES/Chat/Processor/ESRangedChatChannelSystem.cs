using Content.Shared._ES.Chat.Processor.Components;
using Robust.Shared.Player;

namespace Content.Shared._ES.Chat.Processor;

public sealed partial class ESRangedChatChannelSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _entityLookup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESRangedChatChannelComponent, ESGetChatMessageRecipientsEvent>(OnGetRecipients);
    }

    private void OnGetRecipients(Entity<ESRangedChatChannelComponent> ent, ref ESGetChatMessageRecipientsEvent args)
    {
        var xform = Transform(args.Source);
        foreach (var actor in _entityLookup.GetEntitiesInRange<ActorComponent>(xform.Coordinates, ent.Comp.Range))
        {
            args.AddRecipient(actor);
        }
    }
}
