using Content.Shared._ES.Interaction.HoldToFace;
using Content.Shared.Interaction.Events;

namespace Content.Shared._ES.Interaction;

public sealed class ESBlockInteractionSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESBlockInteractionComponent, InteractionAttemptEvent>(OnInteractionAttempt);
    }

    private void OnInteractionAttempt(Entity<ESBlockInteractionComponent> ent, ref InteractionAttemptEvent args)
    {
        args.Cancelled = true;
    }
}
