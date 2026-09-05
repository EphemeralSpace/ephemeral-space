using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Content.Shared.Timing;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Tag;
using Content.Shared.Popups;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared._ES.Announcements;
using Robust.Shared.Prototypes;

using Content.Shared._ES.Hazmat.Components;

namespace Content.Shared._ES.Hazmat;

public abstract partial class ESSharedSanitationChipSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    private static readonly ProtoId<TagPrototype> AirAlarmTag = "AirAlarm";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ESSanitationChipComponent, AfterInteractEvent>(OnSanitationChipAfterInteraction);
    }

    private void OnSanitationChipAfterInteraction(Entity<ESSanitationChipComponent> chip, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { Valid: true } target)
            return;

        if (_tag.HasTag(target, AirAlarmTag))
            args.Handled |= TryDoSanitationChipDoAfter(chip, args.User, target);
    }

    private bool TryDoSanitationChipDoAfter(Entity<ESSanitationChipComponent> chip, EntityUid user, EntityUid target)
    {
        if (_useDelay.IsDelayed(chip.Owner))
            return false;

        var delayTime = chip.Comp.DelayTime;

        var args = new DoAfterArgs(EntityManager, user, delayTime, new ESSanitationChipDoAfterEvent(), chip.Owner, target: target, used: chip.Owner)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
            BreakOnDamage = true,
            NeedHand = true,
            BreakOnHandChange = false,
            MovementThreshold = chip.Comp.MovementThreshold,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return false;

        var userName = Identity.Entity(user, EntityManager);
        // todo fix this
        var popup = "todo POPUP STRING: " + userName;
        _popup.PopupEntity(popup, chip, chip);

        return true;
    }
}
