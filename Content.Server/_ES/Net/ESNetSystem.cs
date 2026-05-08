using System.Numerics;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Wieldable.Components;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._ES.Net;

/// <summary>
///     Handles grabbing items out of the active hand of anyone the net is thrown at,
///     parenting the item during the throw, then unparenting it once the throw ends.
/// </summary>
public sealed partial class ESNetSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESNetComponent, ThrowDoHitEvent>(OnThrowDoHit, after: [typeof(CatchableSystem)]);
        SubscribeLocalEvent<ESNetComponent, StopThrowEvent>(OnStopThrow);
    }

    private void OnThrowDoHit(Entity<ESNetComponent> ent, ref ThrowDoHitEvent args)
    {
        if (!TryComp<HandsComponent>(args.Target, out var hands))
            return;

        if (!_random.Prob(ent.Comp.Chance))
            return;

        if (_hands.GetActiveItem((args.Target, hands)) is not { } activeItem)
            return;

        // can't grab a net with a net everyone knows that
        if (HasComp<ESNetComponent>(activeItem))
            return;

        if (TryComp<WieldableComponent>(activeItem, out var wieldable) && wieldable.Wielded)
            return;

        if (!_hands.TryDrop((args.Target, hands), checkActionBlocker: false))
            return;

        _transform.SetParent(activeItem, ent.Owner);
        _transform.SetLocalPositionRotation(activeItem, Vector2.Zero, Angle.Zero);

        var msg = Loc.GetString("es-net-grabbed-item",
            ("net", ent.Owner),
            ("item", activeItem),
            ("user", Identity.Entity(args.Target, EntityManager)));

        if (ent.Comp.Sound is { } sound)
            _audio.PlayPvs(sound, ent.Owner);

        _popup.PopupEntity(msg, args.Target, Filter.Pvs(args.Target), true, PopupType.SmallCaution);
    }

    private void OnStopThrow(Entity<ESNetComponent> ent, ref StopThrowEvent args)
    {
        // If our net has any children (from yoinking an item)
        // then detach them
        var xform = Transform(ent.Owner);
        var enumerator = xform.ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            _transform.AttachToGridOrMap(child);
            _transform.SetLocalRotation(child, Angle.Zero);
        }
    }
}
