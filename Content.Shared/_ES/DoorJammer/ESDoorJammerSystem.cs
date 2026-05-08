using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Projectiles;

namespace Content.Shared._ES.DoorJammer;

/// <summary>
///     Controls bolting & unbolting a door properly while a door jammer is embedded.
/// </summary>
public sealed partial class ESDoorJammerSystem : EntitySystem
{
    [Dependency] private SharedDoorSystem _door = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESDoorJammerComponent, EmbedEvent>(OnEmbed);
        SubscribeLocalEvent<ESDoorJammerComponent, EmbedDetachEvent>(OnEmbedDetach);
    }

    private void OnEmbed(Entity<ESDoorJammerComponent> ent, ref EmbedEvent args)
    {
        if (!HasComp<DoorComponent>(args.Embedded) || !TryComp<DoorBoltComponent>(args.Embedded, out var doorBolt))
            return;

        ent.Comp.WasAlreadyBolted = _door.IsBolted(args.Embedded, doorBolt);
        Dirty(ent);

        if (ent.Comp.WasAlreadyBolted.Value)
            return;

        _door.TrySetBoltDown((args.Embedded, doorBolt), true, predicted: true);
    }

    private void OnEmbedDetach(Entity<ESDoorJammerComponent> ent, ref EmbedDetachEvent args)
    {
        if (!HasComp<DoorComponent>(args.Embedded) || !TryComp<DoorBoltComponent>(args.Embedded, out var doorBolt))
            return;

        if (ent.Comp.WasAlreadyBolted is false)
            _door.TrySetBoltDown((args.Embedded, doorBolt), false, args.Detacher, predicted: true);;

        ent.Comp.WasAlreadyBolted = null;
        Dirty(ent);
    }
}
