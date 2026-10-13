using System.Numerics;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.Core.Timer.Components;
using Content.Shared.Database;
using Content.Shared.Effects;
using Content.Shared.IdentityManagement;
using Content.Shared.Standing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee;

public abstract partial class SharedMeleeWeaponSystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] protected SharedColorFlashEffectSystem ColorFlashEffect = default!;
    [Dependency] private ESEntityTimerSystem _entityTimer = default!;
    [Dependency] private StandingStateSystem _standingState = default!;

    private static readonly TimeSpan ShoveBuffer = TimeSpan.FromSeconds(0.2f);

    private void InitializeShove()
    {
        SubscribeLocalEvent<PhysicsComponent, ESShovePredictTimerEvent>(OnPredictShove);
    }

    private void OnPredictShove(Entity<PhysicsComponent> ent, ref ESShovePredictTimerEvent args)
    {
        var user = GetEntity(args.User);

        _throwing.TryThrow(
            ent,
            args.Dir,
            10f,
            user.Valid ? user : null,
            compensateFriction: true,
            animated: false,
            playSound: false,
            doSpin: false);
        _status.TrySetStatusEffectDuration(ent, ShoveStatusEffect, ShoveStatusDuration);
    }

    private bool DoShove(EntityUid user, ShoveAttackEvent ev, MeleeWeaponComponent component, ICommonSession? session)
    {
        var target = GetEntity(ev.Target);

        if (Deleted(target) || user == target)
            return false;

        if (MobState.IsIncapacitated(target.Value) ||
            _standingState.IsDown(target.Value))
            return false;

        if (!TryComp<PhysicsComponent>(target, out var body) || body.BodyType == BodyType.Static)
            return false;

        if (!InRange(user, target.Value, component.Range, session))
        {
            return false;
        }

        Interaction.DoContactInteraction(user, target, null, true); // Stellar - Interaction particles
        AdminLogger.Add(LogType.DisarmedAction, $"{ToPrettyString(user):user} shoved {ToPrettyString(target):target}");

        var targetPos = TransformSystem.GetWorldPosition(target.Value);
        var userPos = TransformSystem.GetWorldPosition(user);
        var dir = targetPos != userPos ? (targetPos - userPos).Normalized() : Vector2.Zero;

        var predictEv = new ESShovePredictTimerEvent(GetNetEntity(user), dir);
        _entityTimer.SpawnTimer(target.Value, ShoveBuffer, predictEv);

        if (_net.IsClient)
            ColorFlashEffect.RaiseEffect(Color.Cyan, new List<EntityUid> { target.Value }, Filter.Pvs(target.Value, entityManager: EntityManager));

        var targetEnt = Identity.Entity(target.Value, EntityManager);
        var userEnt = Identity.Entity(user, EntityManager);

        var msgOther = Loc.GetString(
            "disarm-action-shove-popup-message-other-clients",
            ("performerName", userEnt),
            ("targetName", targetEnt));

        var msgUser = Loc.GetString("disarm-action-shove-popup-message-cursor", ("targetName", targetEnt));
        PopupSystem.PopupEntity(msgUser, msgOther, target.Value, user);

        if (TryGetWeapon(user, out var heldWeapon, out var heldWeaponComponent) && heldWeapon != user)
        {
            heldWeaponComponent.NextAttack = Timing.CurTime + component.ShoveDelay;
            DirtyField(heldWeapon, heldWeaponComponent, nameof(MeleeWeaponComponent.NextAttack));
        }

        return true;
    }
}

[Serializable, NetSerializable]
public sealed partial class ESShovePredictTimerEvent : ESEntityTimerEvent
{
    [DataField]
    public NetEntity User;

    [DataField]
    public Vector2 Dir;

    public ESShovePredictTimerEvent(NetEntity user, Vector2 dir) : this()
    {
        User = user;
        Dir = dir;
    }
};
