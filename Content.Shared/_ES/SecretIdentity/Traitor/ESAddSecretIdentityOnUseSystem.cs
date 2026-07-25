using Content.Shared._ES.SecretIdentity.Traitor.Components;
using Content.Shared._ES.SecretIdentity.Traitor.Events;
using Content.Shared._ES.Stagehand;
using Content.Shared._Offbrand.Wounds;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.SecretIdentity.Traitor;

public sealed partial class ESAddSecretIdentityOnUseSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private ESSharedSecretIdentitySystem _secretIdentity = default!;
    [Dependency] private SharedDoAfterSystem _doafter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private RejuvenateSystem _rejuv = default!;
    [Dependency] private HealthRankingSystem _health = default!;
    [Dependency] private ESSharedStagehandNotificationsSystem _stagehandNotifications = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ESAddSecretIdentityOnUseComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<ESAddSecretIdentityOnUseComponent, ESAddSecretIdentityOnUseDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<ESAddSecretIdentityOnUseComponent, ExaminedEvent>(OnExamine);
    }

    private void OnInteract(Entity<ESAddSecretIdentityOnUseComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null)
            return;

        if (!_mind.TryGetMind((EntityUid)args.Target!, out var mind, out var mindComponent)) // No SSD people
            return;

        if (_secretIdentity.GetOrganizationOrNull((mind, mindComponent)) == _proto.Index(ent.Comp.SecretIdentityToAdd).Organization)
            return;

        if (ent.Comp.MindshieldPrevent && HasComp<MindShieldComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.MindshieldedMessage), args.User, args.User);
            return;
        }

        if (ent.Comp.RequireIncapacitated &&
            !_health.IsCritical(args.Target.Value) &&
            _actionBlocker.CanInteract(args.Target.Value, null))
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.NotIncapacitatedMessage), args.User, args.User);
            return;
        }

        if (ent.Comp.Used)
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.UsedMessage), args.User, args.User, PopupType.Medium);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay, new ESAddSecretIdentityOnUseDoAfterEvent(), eventTarget: ent, args.Target, used: ent)
        {
            BlockDuplicate = true,
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnDropItem = true,
            NeedHand = true,
            MovementThreshold = 0.5f,
        };

        _doafter.TryStartDoAfter(doAfterArgs);

        _popup.PopupEntity(Loc.GetString(ent.Comp.UsingMessage), ent, PopupType.MediumCaution);
    }

    private void OnDoAfter(Entity<ESAddSecretIdentityOnUseComponent> ent, ref ESAddSecretIdentityOnUseDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        if (ent.Comp.RequireIncapacitated)
        {
            // TODO ES with offmed this should really be doing something more interesting honestly
            _rejuv.PerformRejuvenate(target);
            // i dont know why you need to do it twice either !
            _rejuv.PerformRejuvenate(target);
        }

        if (!_mind.TryGetMind(target, out var mind, out var mindComponent))
            return;

        var toAddOrganization = _proto.Index(ent.Comp.SecretIdentityToAdd).Organization;

        if (_secretIdentity.GetOrganizationOrNull((mind, mindComponent)) == toAddOrganization)
            return;

        var msg = Loc.GetString(ent.Comp.StagehandNotification,
            ("player", _stagehandNotifications.WrapEntityName(args.User)),
            ("item", ent.Owner),
            ("target", _stagehandNotifications.WrapEntityName(target)));

        _stagehandNotifications.SendStagehandNotification(msg);
        _secretIdentity.ChangeSecretIdentity((mind, mindComponent), ent.Comp.SecretIdentityToAdd);

        ent.Comp.Used = true;
        Dirty(ent);
        args.Handled = true;
    }

    private void OnExamine(Entity<ESAddSecretIdentityOnUseComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (ent.Comp.Used)
            args.PushMarkup(Loc.GetString(ent.Comp.UsedExamineMessage));
        else
            args.PushMarkup(Loc.GetString(ent.Comp.NotUsedExamineMessage));
    }
}
