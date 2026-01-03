using Content.Shared._ES.Emag.Components;
using Content.Shared._ES.Sparks;
using Content.Shared.Access.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Charges.Systems;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Emag;

/// <summary>
/// This handles <see cref="ESEmagComponent"/>
/// </summary>
public sealed class ESEmagSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ESSparksSystem _sparks = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> EmagImmuneTag = "EmagImmune";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESEmagComponent, AfterInteractEvent>(OnAfterInteract);

        SubscribeLocalEvent<LockComponent, ESEmaggedEvent>(OnLockEmagged);
        SubscribeLocalEvent<DoorComponent, ESEmaggedEvent>(OnDoorEmagged);
    }

    private void OnAfterInteract(Entity<ESEmagComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not { } target)
            return;

        args.Handled = TryEmag(ent.AsNullable(), target, args.User);
    }

    private void OnLockEmagged(Entity<LockComponent> ent, ref ESEmaggedEvent args)
    {
        // Emags don't count for things that are already locked.
        if (!ent.Comp.Locked)
            return;

        // Only emag things like lockers, crates, etc.
        if (!HasComp<AccessReaderComponent>(ent))
            return;

        _lock.Unlock(ent, args.User, ent);
        args.Handled = true;
    }

    private void OnDoorEmagged(Entity<DoorComponent> ent, ref ESEmaggedEvent args)
    {
        if (!TryComp<AirlockComponent>(ent, out var airlock))
            return;

        args.Handled = _door.TryOpenAndBolt(ent, ent, airlock);
    }

    public bool TryEmag(Entity<ESEmagComponent?> used, EntityUid target, EntityUid? user = null)
    {
        if (!Resolve(used, ref used.Comp))
            return false;

        if (_tag.HasTag(target, EmagImmuneTag))
            return false;

        if (_charges.IsEmpty(used.Owner))
        {
            if (user != null)
                _popup.PopupClient(Loc.GetString("emag-no-charges"), user.Value, user.Value);
            return false;
        }

        var emaggedEvent = new ESEmaggedEvent(user);
        RaiseLocalEvent(target, ref emaggedEvent);

        if (!emaggedEvent.Handled)
            return false;

        _charges.TryUseCharge(used.Owner);
        if (user.HasValue)
        {
            _popup.PopupPredicted(
                Loc.GetString("emag-success", ("target", Identity.Entity(target, EntityManager))),
                user.Value,
                user,
                PopupType.Medium);
        }

        _sparks.DoSparks(target, user: user, cooldown: false);

        _adminLog.Add(LogType.Emag, LogImpact.High, $"{ToPrettyString(user):player} emagged {ToPrettyString(target):target}");
        return true;
    }
}

[ByRefEvent]
public record struct ESEmaggedEvent(EntityUid? User, bool Handled = false);
