using Content.Shared._ES.Masks.Traitor.Components;
using Content.Shared._ES.Masks.Traitor.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks.Traitor;

public sealed class ESAddMaskOnUseSystem : EntitySystem
{
    [Dependency] private readonly ESSharedMaskSystem _mask = default!;
    [Dependency] private readonly SharedDoAfterSystem _doafter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ESAddMaskOnUseComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<ESAddMaskOnUseComponent, ESAddMaskOnUseDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<ESAddMaskOnUseComponent, ExaminedEvent>(OnExamine);
    }

    private void OnInteract(Entity<ESAddMaskOnUseComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null)
            return;

        if (!_mind.TryGetMind((EntityUid)args.Target!, out var mind, out var mindComponent)) // No SSD people
            return;

        if (_mask.GetTroupeOrNull((mind, mindComponent)) == _proto.Index(ent.Comp.MaskToAdd).Troupe)
            return;

        if (ent.Comp.MindshieldPrevent && HasComp<MindShieldComponent>(args.Target))
        {
            _popup.PopupClient(Loc.GetString(ent.Comp.MindshieldedMessage), args.User, args.User);
            return;
        }

        if (ent.Comp.RequireCrit && !_mobState.IsCritical((EntityUid)args.Target))
        {
            _popup.PopupClient(Loc.GetString(ent.Comp.NotCritMessage), args.User, args.User);
            return;
        }

        if (ent.Comp.Used)
        {
            _popup.PopupClient(Loc.GetString(ent.Comp.UsedMessage), args.User, args.User, PopupType.Medium);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay, new ESAddMaskOnUseDoAfterEvent(), eventTarget: ent, args.Target, used: ent)
        {
            BlockDuplicate = true,
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnDropItem = true,
            NeedHand = true,
            MovementThreshold = 0.5f,
        };

        _doafter.TryStartDoAfter(doAfterArgs);

        _popup.PopupPredicted(Loc.GetString(ent.Comp.UsingMessage), ent, ent, PopupType.MediumCaution);
    }

    private void OnDoAfter(Entity<ESAddMaskOnUseComponent> ent, ref ESAddMaskOnUseDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        if (_mobState.IsCritical(target) && ent.Comp.RequireCrit)
        {
            _damageableSystem.SetAllDamage(target, 0);
        }

        if (!_mind.TryGetMind(target, out var mind, out var mindComponent))
            return;

        var toAddTroupe = _proto.Index(ent.Comp.MaskToAdd).Troupe;

        if (_mask.GetTroupeOrNull((mind, mindComponent)) == toAddTroupe)
            return;

        _mask.RemoveMask((mind, mindComponent));
        _mask.ApplyMask((mind, mindComponent), ent.Comp.MaskToAdd);

        ent.Comp.Used = true;
        Dirty(ent);
        args.Handled = true;
    }

    private void OnExamine(Entity<ESAddMaskOnUseComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (ent.Comp.Used)
            args.PushMarkup(Loc.GetString(ent.Comp.UsedExamineMessage));
        else
            args.PushMarkup(Loc.GetString(ent.Comp.NotUsedExamineMessage));
    }
}
