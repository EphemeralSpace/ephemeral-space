using Content.Shared._ES.Chat.Processor.Components;
using Content.Shared.Examine;
using Robust.Shared.Player;

namespace Content.Shared._ES.Chat.Processor;

public sealed partial class ESRangedChatChannelSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _entityLookup = default!;
    [Dependency] private ExamineSystemShared _examine = default!;

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
            if (ent.Comp.RequireLOS && !_examine.InRangeUnOccluded(args.Source, actor, ent.Comp.Range))
                continue;

            args.AddRecipient(actor);
        }
    }
}
