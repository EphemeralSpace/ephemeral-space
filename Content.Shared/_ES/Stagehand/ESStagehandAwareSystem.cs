using Content.Shared._ES.Stagehand.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Shared._ES.Stagehand;

public sealed class ESStagehandAwareSystem : EntitySystem
{
    [Dependency] private readonly SharedPvsOverrideSystem _pvsOverride = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESStagehandAwareComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ESStagehandComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ESStagehandComponent, PlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnStartup(Entity<ESStagehandAwareComponent> ent, ref ComponentStartup args)
    {
        var query = EntityQueryEnumerator<ESStagehandComponent, ActorComponent>();
        while (query.MoveNext(out _, out var comp))
        {
            _pvsOverride.AddSessionOverride(ent, comp.PlayerSession);
        }
    }

    private void OnPlayerAttached(Entity<ESStagehandComponent> ent, ref PlayerAttachedEvent args)
    {
        var query = EntityQueryEnumerator<ESStagehandAwareComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            _pvsOverride.AddSessionOverride(uid, args.Player);
        }
    }

    private void OnPlayerDetached(Entity<ESStagehandComponent> ent, ref PlayerDetachedEvent args)
    {
        var query = EntityQueryEnumerator<ESStagehandAwareComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            _pvsOverride.RemoveSessionOverride(uid, args.Player);
        }
    }
}
