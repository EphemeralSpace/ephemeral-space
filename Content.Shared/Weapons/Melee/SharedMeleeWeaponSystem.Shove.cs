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
            return false;

        _meleeSound.PlaySwingSound(user, user, component);

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

    private bool DoWideShove(EntityUid user, WideShoveAttackEvent ev, MeleeWeaponComponent component, ICommonSession? session)
    {
        if (!TryComp(user, out TransformComponent? userXform))
            return false;

        var targetMap = TransformSystem.ToMapCoordinates(GetCoordinates(ev.Coordinates));

        if (targetMap.MapId != userXform.MapID)
            return false;

        var userPos = TransformSystem.GetWorldPosition(userXform);
        var direction = targetMap.Position - userPos;
        var distance = Math.Min(component.Range, direction.Length());

        _meleeSound.PlaySwingSound(user, user, component);

        var entities = GetEntityList(ev.Entities);
        if (entities.Count == 0)
            return true;

        // Naughty input
        if (entities.Count > MaxTargets)
        {
            entities.RemoveRange(MaxTargets, entities.Count - MaxTargets);
        }

        // Validate client
        for (var i = entities.Count - 1; i >= 0; i--)
        {
            if (ArcRaySuccessful(entities[i],
                    userPos,
                    direction.ToWorldAngle(),
                    component.ShoveAngle,
                    distance,
                    userXform.MapID,
                    user,
                    session))
            {
                continue;
            }

            // Bad input
            entities.RemoveAt(i);
        }

        var targets = new List<EntityUid>();
        var physicsQuery = GetEntityQuery<PhysicsComponent>();

        foreach (var entity in entities)
        {
            if (entity == user ||
                !physicsQuery.HasComponent(entity))
                continue;

            targets.Add(entity);
        }

        foreach (var target in targets)
        {
            Interaction.DoContactInteraction(user, target, null, true, interactionParticles: false); // Stellar/ES - Interaction particles
        }

        for (var i = targets.Count - 1; i >= 0; i--)
        {
            var target = targets[i];
            if (!Blocker.CanAttack(user, target, (user, component), disarm: true))
            {
                targets.RemoveAt(i);
                continue;
            }

            var targetPos = TransformSystem.GetWorldPosition(target);
            var dir = targetPos != userPos ? (targetPos - userPos).Normalized() : Vector2.Zero;

            var predictEv = new ESShovePredictTimerEvent(GetNetEntity(user), dir);
            _entityTimer.SpawnTimer(target, ShoveBuffer, predictEv);

            if (_net.IsClient)
                ColorFlashEffect.RaiseEffect(Color.Cyan, new List<EntityUid> { target }, Filter.Pvs(target, entityManager: EntityManager));

            var targetEnt = Identity.Entity(target, EntityManager);
            var userEnt = Identity.Entity(user, EntityManager);

            var msgOther = Loc.GetString(
                "disarm-action-shove-popup-message-other-clients",
                ("performerName", userEnt),
                ("targetName", targetEnt));

            var msgUser = Loc.GetString("disarm-action-shove-popup-message-cursor", ("targetName", targetEnt));
            PopupSystem.PopupEntity(msgUser, msgOther, target, user);
        }

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
