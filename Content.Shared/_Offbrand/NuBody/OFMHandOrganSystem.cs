using Content.Shared.Hands.EntitySystems;

namespace Content.Shared._Offbrand.NuBody;

public sealed class OFMHandOrganSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OFMHandOrganComponent, OrganGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<OFMHandOrganComponent, OrganGotRemovedEvent>(OnGotRemoved);
    }

    private void OnGotInserted(Entity<OFMHandOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        _hands.AddHand(args.Target, ent.Comp.HandID, ent.Comp.Data);
    }

    private void OnGotRemoved(Entity<OFMHandOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        _hands.RemoveHand(args.Target, ent.Comp.HandID);
    }
}
