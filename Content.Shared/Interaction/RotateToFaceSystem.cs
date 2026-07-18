using System.Numerics;
using Content.Shared._ES.Interaction.HoldToFace;
using Content.Shared.ActionBlocker;
using Content.Shared.Buckle.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Rotatable;
using JetBrains.Annotations;

namespace Content.Shared.Interaction
{
    /// <summary>
    /// Contains common code used to rotate a player to face a given target or direction.
    /// This interaction in itself is useful for various roleplay purposes.
    /// But it needs specialized code to handle chairs and such.
    /// Doesn't really fit with SharedInteractionSystem so it's not there.
    /// </summary>
    [UsedImplicitly]
    public sealed partial class RotateToFaceSystem : EntitySystem
    {
        [Dependency] private ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private SharedTransformSystem _transform = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ESForcedFacingComponent, ESRefreshNoRotateOnMoveEvent>(OnRefreshNoRotateOnMove);
        }

        private void OnRefreshNoRotateOnMove(Entity<ESForcedFacingComponent> ent, ref ESRefreshNoRotateOnMoveEvent args)
        {
            args.Enabled = true;
        }

        /// <summary>
        /// Tries to rotate the entity towards the target rotation. Returns false if it needs to keep rotating.
        /// </summary>
        public bool TryRotateTo(EntityUid uid,
            Angle goalRotation,
            float frameTime,
            Angle tolerance,
            double rotationSpeed = float.MaxValue,
            TransformComponent? xform = null)
        {
            if (!Resolve(uid, ref xform))
                return true;

            // If we have a max rotation speed then do that.
            // We'll rotate even if we can't shoot, looks better.
            if (rotationSpeed < float.MaxValue)
            {
                var worldRot = _transform.GetWorldRotation(xform);

                var rotationDiff = Angle.ShortestDistance(worldRot, goalRotation).Theta;
                var maxRotate = rotationSpeed * frameTime;

                if (Math.Abs(rotationDiff) > maxRotate)
                {
                    var goalTheta = worldRot + Math.Sign(rotationDiff) * maxRotate;
                    TryFaceAngle(uid, goalTheta, xform);
                    rotationDiff = (goalRotation - goalTheta);

                    if (Math.Abs(rotationDiff) > tolerance)
                    {
                        return false;
                    }

                    return true;
                }

                TryFaceAngle(uid, goalRotation, xform);
            }
            else
            {
                TryFaceAngle(uid, goalRotation, xform);
            }

            return true;
        }

        public bool TryFaceCoordinates(EntityUid user, Vector2 coordinates, TransformComponent? xform = null)
        {
            if (!Resolve(user, ref xform))
                return false;

            var diff = coordinates - _transform.GetMapCoordinates(user, xform: xform).Position;
            if (diff.LengthSquared() <= 0.01f)
                return true;

            var diffAngle = Angle.FromWorldVec(diff);
            return TryFaceAngle(user, diffAngle);
        }

        public bool TryFaceAngle(EntityUid user, Angle diffAngle, TransformComponent? xform = null)
        {
            if (!_actionBlockerSystem.CanChangeDirection(user))
                return false;

            if (TryComp(user, out BuckleComponent? buckle) && buckle.BuckledTo is {} strap)
            {
                // What if a person is strapped to a borg?
                // I'm pretty sure this would allow them to be partially ratatouille'd

                // We're buckled to another object. Is that object rotatable?
                if (!TryComp<RotatableComponent>(strap, out var rotatable) || !rotatable.RotateWhileAnchored)
                    return false;

                // Note the assumption that even if unanchored, user can only do spinnychair with an "independent wheel".
                // (Since the user being buckled to it holds it down with their weight.)
                // This is logically equivalent to RotateWhileAnchored.
                // Barstools and office chairs have independent wheels, while regular chairs don't.
                _transform.SetWorldRotation(Transform(strap), diffAngle);
                return true;
            }

            // user is not buckled in; apply to their transform
            if (!Resolve(user, ref xform))
                return false;

            _transform.SetWorldRotation(xform, diffAngle);
            return true;
        }

        public void RefreshNoRotateOnMove(EntityUid uid)
        {
            var ev = new ESRefreshNoRotateOnMoveEvent();
            RaiseLocalEvent(uid, ref ev);

            if (ev.Enabled)
            {
                EnsureComp<NoRotateOnMoveComponent>(uid);
            }
            else
            {
                RemComp<NoRotateOnMoveComponent>(uid);
            }
        }

        public void StartFacing(Entity<ESForcedFacingComponent?> ent, EntityUid target)
        {
            if (Resolve(ent, ref ent.Comp, false) && ent.Comp.Targets.Contains(target))
                return;

            EnsureComp<NoRotateOnInteractComponent>(ent);

            var facing = EnsureComp<ESForcedFacingComponent>(ent);
            facing.Targets.Add(target);
            Dirty(ent, facing);

            var facingTarget = EnsureComp<ESForcedFacingTargetComponent>(target);
            facingTarget.Facing.Add(ent);
            Dirty(target, facingTarget);

            RefreshNoRotateOnMove(ent);
        }

        public void StopFacing(Entity<ESForcedFacingComponent?> ent, Entity<ESForcedFacingTargetComponent?> target)
        {
            if (!Resolve(ent, ref ent.Comp, false) || !ent.Comp.Targets.Contains(target))
                return;

            RemComp<NoRotateOnInteractComponent>(ent);

            ent.Comp.Targets.Remove(target);
            Dirty(ent);

            if (Resolve(target, ref target.Comp, false))
            {
                target.Comp.Facing.Remove(ent);
                Dirty(target, target.Comp);

                if (target.Comp.Facing.Count == 0)
                    RemComp(target, target.Comp);
            }

            if (!ent.Comp.PrimaryTarget.HasValue)
            {
                RemComp(ent, ent.Comp);
                RefreshNoRotateOnMove(ent);
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var (uid, comp) in EntityQueryEnumerator<ESForcedFacingComponent>())
            {
                if (comp.PrimaryTarget is not { } target)
                    continue;

                if (!Exists(target))
                    continue;

                var targetCoords = _transform.GetMapCoordinates(target).Position;
                TryFaceCoordinates(uid, targetCoords);
            }
        }
    }

    [ByRefEvent]
    public record struct ESRefreshNoRotateOnMoveEvent(bool Enabled = false);
}
