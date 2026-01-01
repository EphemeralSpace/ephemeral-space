using Robust.Shared.Containers;

namespace Content.Shared._Offbrand.NuBody;

public sealed partial class OFMBodySystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private EntityQuery<OFMBodyComponent> _bodyQuery;
    private EntityQuery<OFMOrganComponent> _organQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OFMBodyComponent, ComponentInit>(OnBodyInit);
        SubscribeLocalEvent<OFMBodyComponent, ComponentShutdown>(OnBodyShutdown);

        SubscribeLocalEvent<OFMBodyComponent, EntInsertedIntoContainerMessage>(OnBodyEntInserted);
        SubscribeLocalEvent<OFMBodyComponent, EntRemovedFromContainerMessage>(OnBodyEntRemoved);

        _bodyQuery = GetEntityQuery<OFMBodyComponent>();
        _organQuery = GetEntityQuery<OFMOrganComponent>();
    }

    private void OnBodyInit(Entity<OFMBodyComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Organs =
            _container.EnsureContainer<Container>(ent, OFMBodyComponent.ContainerID);
    }

    private void OnBodyShutdown(Entity<OFMBodyComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Organs is { } organs)
            _container.ShutdownContainer(organs);
    }

    private void OnBodyEntInserted(Entity<OFMBodyComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != OFMBodyComponent.ContainerID)
            return;

        if (!_organQuery.TryComp(args.Entity, out var organ))
            return;

        var body = new OrganInsertedIntoEvent(args.Entity);
        RaiseLocalEvent(ent, ref body);

        var ev = new OrganGotInsertedEvent(ent);
        RaiseLocalEvent(args.Entity, ref ev);

        if (organ.Body != ent)
        {
            organ.Body = ent;
            Dirty(args.Entity, organ);
        }
    }

    private void OnBodyEntRemoved(Entity<OFMBodyComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != OFMBodyComponent.ContainerID)
            return;

        if (!_organQuery.TryComp(args.Entity, out var organ))
            return;

        var body = new OrganRemovedFromEvent(args.Entity);
        RaiseLocalEvent(ent, ref body);

        var ev = new OrganGotRemovedEvent(ent);
        RaiseLocalEvent(args.Entity, ref ev);

        if (organ.Body == null)
            return;

        organ.Body = null;
        Dirty(args.Entity, organ);
    }
}
