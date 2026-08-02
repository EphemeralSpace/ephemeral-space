using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Climbing.Systems;
using Content.Shared.Disposal.Holder;
using Content.Shared.Disposal.Unit;
using Content.Shared.DoAfter;
using Content.Shared.Eye;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._DV.NodeCrawl;

/// <summary>
/// Manages entry & exit of node crawlers into node networks
/// </summary>
public abstract partial class SharedNodeCrawlSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedMoverController _mover = default!;
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedEyeSystem _eye = default!;
    [Dependency] private SharedActionsSystem _action = default!;
    [Dependency] private NodeCrawlerMovementSystem _nodeCrawler = default!;
    [Dependency] private ClimbSystem _climb = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDisposalHolderSystem _disposal = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private const string MoverContainer = "mover-container";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StartNodeCrawlActionEvent>(OnStartNodeCrawlAction);
        SubscribeLocalEvent<NodeCrawlerComponent, ComponentStartup>(OnCrawlerStartup);
        SubscribeLocalEvent<NodeCrawlerComponent, NodeCrawlEnterDoAfterEvent>(OnNodeCrawlEntryDoAfter);
        SubscribeLocalEvent<NodeCrawlerComponent, NodeCrawlerArrivedAtNodeEvent>(OnArrivedAtNode);
        SubscribeLocalEvent<NodeCrawlerComponent, GetVisMaskEvent>(OnGetVisMask);

        SubscribeLocalEvent<CrawlableNodeComponent, ComponentShutdown>(OnCrawlableShutdown);
        SubscribeLocalEvent<NodeCrawlerMovementComponent, ComponentShutdown>(OnMovementShutdown);
        SubscribeLocalEvent<NodeCrawlerComponent, ComponentShutdown>(OnCrawlerShutdown);

        SubscribeLocalEvent<CrawlableNodeComponent, AnchorStateChangedEvent>(OnCrawlableAnchorChanged);

        SubscribeLocalEvent<CrawlableNodeComponent, DisposalTubeHolderEntered>(OnCrawlableDisposalHolderEntered);
    }

    private void OnStartNodeCrawlAction(StartNodeCrawlActionEvent args)
    {
        var user = args.Performer;
        var target = args.Target;

        if (!TryComp<NodeCrawlerComponent>(user, out var nodeCrawler))
            return;

        if (!_entityWhitelist.IsWhitelistPass(nodeCrawler.ExitNodes, target))
            return;

        if (_inventory.TryGetContainerSlotEnumerator(args.Performer,
                out var enumerator,
                nodeCrawler.RequiredEmptySlots))
        {
            while (enumerator.MoveNext(out var slot))
            {
                if (slot.Count == 0)
                    continue;

                _popup.PopupEntity(Loc.GetString(nodeCrawler.EmptySlotsPopupMessage), user, user);
                return;
            }
        }

        StartEntryDoAfter((user, nodeCrawler), target);
        args.Handled = true;
    }

    private void StartEntryDoAfter(Entity<NodeCrawlerComponent> ent, EntityUid target)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager, ent.Owner, ent.Comp.EnterDelay, new NodeCrawlEnterDoAfterEvent(), ent.Owner, target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnNodeCrawlEntryDoAfter(Entity<NodeCrawlerComponent> ent, ref NodeCrawlEnterDoAfterEvent args)
    {
        if (args.Cancelled || args.Target is not { } target)
            return;

        NodeCrawl(ent, target);
    }

    private void OnCrawlerStartup(Entity<NodeCrawlerComponent> ent, ref ComponentStartup args)
    {
        _action.AddAction(ent.Owner, ent.Comp.Action);
    }

    protected virtual void SetupAir(Entity<NodeCrawlerMovementComponent> movement)
    {
    }

    protected virtual void EjectAir(Entity<NodeCrawlerMovementComponent> movement)
    {
    }

    private void NodeCrawl(Entity<NodeCrawlerComponent> ent, EntityUid target)
    {
        if (!_net.IsServer)
            return;

        _audio.PlayPvs(ent.Comp.StartSound, target);

        var mover = Spawn(ent.Comp.MoverEntity, Transform(target).Coordinates);
        var crawler = Comp<NodeCrawlerMovementComponent>(mover);

        var container = _container.GetContainer(mover, MoverContainer);
        _container.Insert(ent.Owner, container);

        ent.Comp.Mover = mover;
        Dirty(ent);

        var evt = new NodeCrawlerStartedCrawlingEvent((mover, crawler));
        RaiseLocalEvent(ent, ref evt);

        _nodeCrawler.SetNode((mover, crawler), target);
        _nodeCrawler.SetHeldCrawler((mover, crawler), ent);

        SetupAir((mover, crawler));

        _mover.SetRelay(ent, mover);
        _physics.SetCanCollide(ent.Owner, false);
        _physics.SetCanCollide(mover, false);
        _eye.RefreshVisibilityMask(ent.Owner);
    }

    /// <summary>
    /// Causes this node crawler to exit its node crawl.
    /// </summary>
    /// <param name="ent">The crawler to exit node-crawl from.</param>
    public void ExitNodeCrawl(Entity<NodeCrawlerComponent> ent)
    {
        if (ent.Comp.Mover is not { } mover)
            return;

        ent.Comp.Mover = null;
        Dirty(ent);

        var container = _container.GetContainer(mover, MoverContainer);
        _container.Remove(ent.Owner, container);

        foreach (var other in _container.EmptyContainer(container))
        {
            if (!TryComp<NodeCrawlerComponent>(other, out var otherCrawler))
                continue;

            otherCrawler.Mover = null;
            Dirty(other, otherCrawler);
        }

        RemComp<RelayInputMoverComponent>(ent);
        if (_net.IsServer && !TerminatingOrDeleted(mover))
        {
            if (TryComp<NodeCrawlerMovementComponent>(mover, out var movement))
                EjectAir((mover, movement));

            QueueDel(mover); // deletion isn't predicted because client queued deletion doesn't interact well with container stuff
        }

        _physics.SetCanCollide(ent.Owner, true);
        _eye.RefreshVisibilityMask(ent.Owner);
    }

    private void OnArrivedAtNode(Entity<NodeCrawlerComponent> ent, ref NodeCrawlerArrivedAtNodeEvent args)
    {
        if (!_entityWhitelist.IsWhitelistPass(ent.Comp.ExitNodes, args.Node))
            return;

        ExitNodeCrawl(ent);
        _climb.Climb(ent.Owner, ent.Owner, args.Node, true);
    }

    private void OnGetVisMask(Entity<NodeCrawlerComponent> ent, ref GetVisMaskEvent args)
    {
        if (ent.Comp.Mover is null)
            return;

        args.VisibilityMask |= (int)VisibilityFlags.Subfloor;
    }

    private void OnCrawlableShutdown(Entity<CrawlableNodeComponent> ent, ref ComponentShutdown args)
    {
        foreach (var crawler in ent.Comp.Crawlers)
        {
            if (TerminatingOrDeleted(crawler))
                continue;

            var movement = Comp<NodeCrawlerMovementComponent>(crawler);
            if (movement.HeldCrawler is not { } held)
                continue;

            _nodeCrawler.SetNode((crawler, movement), null);
            ExitNodeCrawl((held, Comp<NodeCrawlerComponent>(held)));
        }
    }

    private void OnMovementShutdown(Entity<NodeCrawlerMovementComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Node is { } node && !TerminatingOrDeleted(node))
        {
            var nodeComp = Comp<CrawlableNodeComponent>(node);
            nodeComp.Crawlers.Remove(ent);
            Dirty(node, nodeComp);
        }

        if (ent.Comp.HeldCrawler is { } crawler && !TerminatingOrDeleted(crawler) && TryComp<NodeCrawlerComponent>(crawler, out var nodeCrawler))
        {
            ExitNodeCrawl((crawler, nodeCrawler));
        }
    }

    private void OnCrawlerShutdown(Entity<NodeCrawlerComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        ExitNodeCrawl(ent);
    }

    private void OnCrawlableAnchorChanged(Entity<CrawlableNodeComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            return;

        if (TerminatingOrDeleted(ent.Owner))
            return;

        foreach (var crawler in ent.Comp.Crawlers)
        {
            var movement = Comp<NodeCrawlerMovementComponent>(crawler);
            if (movement.HeldCrawler is not { } held)
                continue;

            ExitNodeCrawl((held, Comp<NodeCrawlerComponent>(held)));
        }
    }

    private void OnCrawlableDisposalHolderEntered(Entity<CrawlableNodeComponent> ent, ref DisposalTubeHolderEntered args)
    {
        // if a disposal holder enters the tube that a crawler is in,
        // the crawler will be force-exited from the crawl and placed inside the disposal holder
        if (ent.Comp.Crawlers.Count == 0 || args.Holder.Comp.Container is not { } container)
            return;

        foreach (var movementEntity in ent.Comp.Crawlers)
        {
            if (!TryComp<NodeCrawlerMovementComponent>(movementEntity, out var movement))
                continue;

            if (movement.HeldCrawler is not { } crawler || !TryComp<NodeCrawlerComponent>(movement.HeldCrawler, out var crawlerComp))
                continue;

            _audio.PlayPvs(crawlerComp.ForceStopSound, crawler);
            ExitNodeCrawl((crawler, crawlerComp));
            _container.Insert(crawler, container);
            _disposal.AttachEntity(args.Holder, crawler);
        }
    }
}
