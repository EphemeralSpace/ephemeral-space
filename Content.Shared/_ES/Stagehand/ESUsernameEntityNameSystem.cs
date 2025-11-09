using Content.Shared._ES.Stagehand.Components;
using Robust.Shared.Player;

namespace Content.Shared._ES.Stagehand;

public sealed class ESUsernameEntityNameSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESUsernameEntityNameComponent, PlayerAttachedEvent>(OnPlayerAttached);
    }
    private void OnPlayerAttached(Entity<ESUsernameEntityNameComponent> ent, ref PlayerAttachedEvent args)
    {
        _metaData.SetEntityName(ent, args.Player.Name);
    }
}
