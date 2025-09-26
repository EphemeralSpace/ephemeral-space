using System.Linq;
using Content.Shared._ES.Cargo.Storeroom.Components;
using Content.Shared.Stacks;
using Content.Shared.Station;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._ES.Cargo.Storeroom;

public abstract class ESSharedStoreroomSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;

    private readonly HashSet<Entity<ESStoreroomPalletComponent, TransformComponent>> _pallets = new();
    private readonly HashSet<EntityUid> _palletGoods = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESStoreroomPalletComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<ESStoreroomPalletComponent, EndCollideEvent>(OnEndCollide);

        SubscribeLocalEvent<ESPalletTrackerComponent, EntityRenamedEvent>(OnEntityRenamed);
        SubscribeLocalEvent<ESPalletTrackerComponent, StorageAfterOpenEvent>(OnStorageAfterOpen);
        SubscribeLocalEvent<ESPalletTrackerComponent, StorageAfterCloseEvent>(OnStorageAfterClose);
    }

    private void OnStartCollide(Entity<ESStoreroomPalletComponent> ent, ref StartCollideEvent args)
    {
        if (_entityWhitelist.IsWhitelistFail(ent.Comp.GoodsWhitelist, args.OtherEntity))
            return;

        if (!Transform(ent).Anchored)
            return;

        if (_station.GetOwningStation(ent) is not { } station)
            return;
        EnsureComp<ESPalletTrackerComponent>(args.OtherEntity);
        UpdateStationStock(station);
    }

    private void OnEndCollide(Entity<ESStoreroomPalletComponent> ent, ref EndCollideEvent args)
    {
        if (_entityWhitelist.IsWhitelistFail(ent.Comp.GoodsWhitelist, args.OtherEntity))
            return;

        if (!Transform(ent).Anchored)
            return;

        if (_station.GetOwningStation(ent) is not { } station)
            return;
        RemCompDeferred<ESPalletTrackerComponent>(args.OtherEntity);
        UpdateStationStock(station);
    }

    private void OnEntityRenamed(Entity<ESPalletTrackerComponent> ent, ref EntityRenamedEvent args)
    {
        if (_station.GetOwningStation(ent) is not { } station)
            return;
        UpdateStationStock(station);
    }

    private void OnStorageAfterOpen(Entity<ESPalletTrackerComponent> ent, ref StorageAfterOpenEvent args)
    {
        if (_station.GetOwningStation(ent) is not { } station)
            return;
        UpdateStationStock(station);
    }

    private void OnStorageAfterClose(Entity<ESPalletTrackerComponent> ent, ref StorageAfterCloseEvent args)
    {
        if (_station.GetOwningStation(ent) is not { } station)
            return;
        UpdateStationStock(station);
    }

    public void UpdateStationStock(Entity<ESStoreroomStationComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;
        ent.Comp.Stock = GetStoreroomStock(ent);
        Dirty(ent);
    }

    public HashSet<Entity<ESStoreroomPalletComponent, TransformComponent>> GetStoreroomPallets(EntityUid station)
    {
        _pallets.Clear();

        var query = EntityQueryEnumerator<ESStoreroomPalletComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (!xform.Anchored)
                continue;

            if (_station.GetOwningStation(uid, xform) != station)
                continue;

            _pallets.Add((uid, comp, xform));
        }

        return _pallets;
    }

    public Dictionary<ESStoreroomContainerEntry, int> GetStoreroomStock(EntityUid station)
    {
        var containers = new Dictionary<ESStoreroomContainerEntry, int>();
        var processed = new HashSet<EntityUid>();

        foreach (var pallet in GetStoreroomPallets(station))
        {
            _palletGoods.Clear();
            _physics.GetContactingEntities(pallet.Owner, _palletGoods);

            foreach (var palletGood in _palletGoods)
            {
                if (!processed.Add(palletGood))
                    continue;

                if (_entityWhitelist.IsWhitelistFail(pallet.Comp1.GoodsWhitelist, palletGood))
                    continue;

                var container = CreateContainerEntry(palletGood);
                if (!containers.TryAdd(container, 1))
                    containers[container] += 1;
            }
        }

        return containers;
    }

    private ESStoreroomContainerEntry CreateContainerEntry(EntityUid palletGood)
    {
        var meta = MetaData(palletGood);
        var container = new ESStoreroomContainerEntry(meta.EntityPrototype?.ID, meta.EntityName);

        EntityStorageComponent? entityStorage = null;
        if (_entityStorage.ResolveStorage(palletGood, ref entityStorage))
        {
            foreach (var content in entityStorage.Contents.ContainedEntities)
            {
                var contentMeta = MetaData(content);
                if (container.Contents.FirstOrDefault(e =>
                        e.Name.Equals(contentMeta.EntityName, StringComparison.InvariantCultureIgnoreCase))
                    is { } existingEntry)
                {
                    existingEntry.Count += _stack.GetCount(content);
                }
                else
                {
                    var entry = new ESStoreroomEntry(contentMeta.EntityPrototype?.ID, contentMeta.EntityName);
                    entry.Count = _stack.GetCount(content);
                    container.Contents.Add(entry);
                }
            }
        }

        return container;
    }
}
