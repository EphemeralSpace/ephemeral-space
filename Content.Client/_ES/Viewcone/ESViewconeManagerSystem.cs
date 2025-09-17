using Content.Client._ES.Viewcone;
using Content.Shared._ES.Viewcone;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._ES.Viewcone;

public sealed class ESViewconeManagerSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    private ESViewconeConeOverlay _coneOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESViewconeManagerComponent, ComponentInit>(OnConeManInit);
        SubscribeLocalEvent<ESViewconeManagerComponent, ComponentShutdown>(OnConeManShutdown);

        SubscribeLocalEvent<ESViewconeManagerComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ESViewconeManagerComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<ESViewconeManagerComponent, ViewconeUpdateEvent>(OnViewconeUpdate);

        _coneOverlay = new();
    }

    private void OnPlayerAttached(Entity<ESViewconeManagerComponent> entity, ref LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_coneOverlay);
    }

    private void OnPlayerDetached(Entity<ESViewconeManagerComponent> entity, ref LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_coneOverlay);
        ResetOccludedAlpha();
    }

    private void OnConeManInit(Entity<ESViewconeManagerComponent> entity, ref ComponentInit args)
    {
        if (_playerManager.LocalSession?.AttachedEntity == entity.Owner)
            _overlayMan.AddOverlay(_coneOverlay);
    }

    private void OnConeManShutdown(Entity<ESViewconeManagerComponent> entity, ref ComponentShutdown args)
    {
        if (_playerManager.LocalSession?.AttachedEntity == entity.Owner)
        {
            _overlayMan.RemoveOverlay(_coneOverlay);
            ResetOccludedAlpha();
        }
    }

    private void ResetOccludedAlpha()
    {
        var query = AllEntityQuery<ESViewconeOccludedComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!_entityManager.TryGetComponent<SpriteComponent>(uid, out var sprite))
                continue;

            sprite.Color = sprite.Color.WithAlpha(comp.BaseAlpha);
        }
    }

    private void OnViewconeUpdate(Entity<ESViewconeManagerComponent> entity, ref ViewconeUpdateEvent args)
    {
        UpdateViewcone(entity);
    }

    public void UpdateViewcone(Entity<ESViewconeManagerComponent> entity)
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
