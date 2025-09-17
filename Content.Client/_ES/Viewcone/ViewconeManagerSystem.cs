using Content.Client._ES.Viewcone;
using Content.Shared._ES.Viewcone;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._ES.Viewcone;

public sealed class ViewconeManagerSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    private ViewconeOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ViewconeManagerComponent, ComponentInit>(OnConeManInit);
        SubscribeLocalEvent<ViewconeManagerComponent, ComponentShutdown>(OnConeManShutdown);

        SubscribeLocalEvent<ViewconeManagerComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ViewconeManagerComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<ViewconeManagerComponent, ViewconeUpdateEvent>(OnViewconeUpdate);

        _overlay = new();
    }

    private void OnPlayerAttached(Entity<ViewconeManagerComponent> entity, ref LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(Entity<ViewconeManagerComponent> entity, ref LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
        ResetOccludedAlpha();
    }

    private void OnConeManInit(Entity<ViewconeManagerComponent> entity, ref ComponentInit args)
    {
        if (_playerManager.LocalSession?.AttachedEntity == entity.Owner)
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnConeManShutdown(Entity<ViewconeManagerComponent> entity, ref ComponentShutdown args)
    {
        if (_playerManager.LocalSession?.AttachedEntity == entity.Owner)
        {
            _overlayMan.RemoveOverlay(_overlay);
            ResetOccludedAlpha();
        }
    }

    private void ResetOccludedAlpha()
    {
        var query = AllEntityQuery<ViewconeOccludedComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!_entityManager.TryGetComponent<SpriteComponent>(uid, out var sprite))
                continue;

            sprite.Color = sprite.Color.WithAlpha(comp.BaseAlpha);
        }
    }

    private void OnViewconeUpdate(Entity<ViewconeManagerComponent> entity, ref ViewconeUpdateEvent args)
    {
        UpdateViewcone(entity);
    }

    public void UpdateViewcone(Entity<ViewconeManagerComponent> entity)
    {

    }
}

public sealed class ViewconeUpdateEvent : EntityEventArgs
{
    public readonly EntityUid Entity;

    public ViewconeUpdateEvent(EntityUid entity)
    {
        Entity = entity;
    }
}
