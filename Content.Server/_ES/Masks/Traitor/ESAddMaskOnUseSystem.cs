using Content.Server._ES.Masks.Traitor.Components;
using Content.Server.Database.Migrations.Sqlite;
using Content.Shared._ES.Masks.Components;
using Content.Shared._ES.Masks.Traitor.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.EntityEffects;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Microsoft.CodeAnalysis;
using Robust.Shared.Player;

namespace Content.Server._ES.Masks.Traitor;

public sealed class ESAddMaskOnUseSystem : EntitySystem
{
    [Dependency] private readonly ESMaskSystem _mask = default!;
    [Dependency] private readonly SharedDoAfterSystem _doafter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ESAddMaskOnUseComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<ESAddMaskOnUseComponent, ESAddMaskOnUseDoAfterEvent>(OnDoAfter);
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

        if (_mask.GetTroupeOrNull((mind, mindComponent)) == component.TroupeToAdd)
            return;

        if (component.Used)
        {
            _popup.PopupEntity(Loc.GetString("subverter-chip-used"), args.User, args.User, PopupType.Medium);
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

        _popup.PopupEntity(Loc.GetString("subverter-chip-implanting"), uid, PopupType.MediumCaution);
    }

    private void OnDoAfter(EntityUid uid, ESAddMaskOnUseComponent component, ESAddMaskOnUseDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (args.Target is not { } target)
            return;

        if (_mobState.IsCritical(target) && component.RequireCrit)
        {
            _damageableSystem.SetAllDamage(target, 0);
        }

        if (!_mind.TryGetMind(target, out var mind, out var mindComponent))
            return;

        if (!_mask.TryGetTroupeEntity(component.TroupeToAdd, out var troupe))
            return;

        _mask.ApplyMask((mind, mindComponent), component.MaskToAdd, (Entity<ESTroupeRuleComponent>)troupe);

        if (!TryComp<ActorComponent>(args.Target, out var actor))
            return;

        component.Targets!.Add(actor.PlayerSession);

        if (component.Targets! != null)
            _mask.TryAssignToTroupe((Entity<ESTroupeRuleComponent>)troupe, ref component.Targets); // Apply mask doesnt assing troupe as well

        component.Used = true;
        args.Handled = true;
    }
}
