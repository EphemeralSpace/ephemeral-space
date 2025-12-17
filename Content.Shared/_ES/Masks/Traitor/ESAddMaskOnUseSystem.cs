using Content.Server._ES.Masks.Traitor.Components;
using Content.Shared._ES.Masks;
using Content.Shared._ES.Masks.Components;
using Content.Shared._ES.Masks.Traitor.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Microsoft.CodeAnalysis;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Traitor;

public sealed class ESAddMaskOnUseSystem : EntitySystem
{
    [Dependency] private readonly ESSharedMaskSystem _mask = default!;
    [Dependency] private readonly SharedDoAfterSystem _doafter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly PrototypeManager _proto = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<ESAddMaskOnUseComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<ESAddMaskOnUseComponent, ESAddMaskOnUseDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<ESAddMaskOnUseComponent, ExaminedEvent>(OnExamine);
    }

    private void OnInteract(EntityUid uid, ESAddMaskOnUseComponent component, ref AfterInteractEvent args)
    {
        if (args.Target == null)
            return;

        if (!_mobState.IsCritical((EntityUid)args.Target) && component.RequireCrit)
            return;

        if (HasComp<MindShieldComponent>(args.Target) && component.MindshieldPrevent)
            return;

        if (!_mind.TryGetMind((EntityUid)args.Target!, out var mind, out var mindComponent)) // No SSD people
            return;

        _mask.TryGetTroupe((mind, mindComponent), out var troupe);

        if (troupe == _proto.Index(component.MaskToAdd).Troupe)
            return;

        if (component.Used)
        {
            _popup.PopupEntity(Loc.GetString(component.UsedMessage), args.User, args.User, PopupType.Medium);
            return;
        }

        var DoAfterArgs = new DoAfterArgs(EntityManager, args.User, component.Delay, new ESAddMaskOnUseDoAfterEvent(), eventTarget: uid, args.Target, used: uid)
        {
            BlockDuplicate = true,
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnDropItem = true,
            NeedHand = true,
            MovementThreshold = 0.5f
        };

        _doafter.TryStartDoAfter(DoAfterArgs);

        _popup.PopupEntity(Loc.GetString(component.UsingMessage), uid, PopupType.MediumCaution);

    }
    private void OnDoAfter(EntityUid uid, ESAddMaskOnUseComponent component, ESAddMaskOnUseDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var target = (EntityUid)args.Target!;

        if (_mobState.IsCritical(target) && component.RequireCrit)
        {
            _damageableSystem.SetAllDamage(target, 0);
        }

        if (!_mind.TryGetMind(target, out var mind, out var mindComponent))
            return;

        var Troupe = _proto.Index(component.MaskToAdd).Troupe;

        if (!_mask.TryGetTroupeEntity(Troupe, out var troupe))
            return;

        if (_mask.GetTroupeOrNull((mind, mindComponent)) == Troupe)
            return;

        _mask.ApplyMask((mind, mindComponent), component.MaskToAdd, (Entity<ESTroupeRuleComponent>)troupe);

        if (!TryComp<ActorComponent>(args.Target, out var actor))
            return;

        component.Used = true;
        args.Handled = true;
    }

    private void OnExamine(EntityUid uid, ESAddMaskOnUseComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (component.Used)
            args.PushMarkup(Loc.GetString(component.NotUsedExamineMessage), 1);
    }
}
