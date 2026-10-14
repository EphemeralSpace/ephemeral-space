using System.Linq;
using Content.Client.Gameplay;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Client.Weapons.Melee;

public sealed partial class MeleeWeaponSystem : SharedMeleeWeaponSystem
{
    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IStateManager _stateManager = default!;
    [Dependency] private AnimationPlayerSystem _animation = default!;
    [Dependency] private InputSystem _inputSystem = default!;
    [Dependency] private MapSystem _map = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    [Dependency] private EntityQuery<TransformComponent> _xformQuery;

    private const string MeleeLungeKey = "melee-lunge";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<MeleeLungeEvent>(OnMeleeLunge);
        UpdatesOutsidePrediction = true;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        UpdateEffects();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Timing.IsFirstTimePredicted)
            return;

        if (_player.LocalEntity is not { } entity)
            return;

        if (!TryGetWeapon(entity, out var weapon))
            return;

        if (!CombatMode.IsInCombatMode(entity) || !Blocker.CanAttack(entity, weapon: weapon))
        {
            weapon.Value.Comp.Attacking = false;
            return;
        }

        var useDown = _inputSystem.CmdStates.GetState(EngineKeyFunctions.Use);
        var altDown = _inputSystem.CmdStates.GetState(EngineKeyFunctions.UseSecondary);

        if (weapon.Value.Comp.AutoAttack || useDown != BoundKeyState.Down && altDown != BoundKeyState.Down)
        {
            if (weapon.Value.Comp.Attacking)
            {
                RaisePredictiveEvent(new StopAttackEvent(GetNetEntity(weapon.Value)));
            }
        }

        if (weapon.Value.Comp.Attacking || weapon.Value.Comp.NextAttack > Timing.CurTime)
            return;

        // TODO using targeted actions while combat mode is enabled should NOT trigger attacks.

        var mousePos = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);
        if (mousePos.MapId == MapId.Nullspace)
            return;

        var coordinates = MapManager.TryFindGridAt(mousePos, out var gridUid, out _)
            ? TransformSystem.ToCoordinates(gridUid, mousePos)
            : TransformSystem.ToCoordinates(_map.GetMap(mousePos.MapId), mousePos);

        // secondary attack
        if (altDown == BoundKeyState.Down && weapon.Value.Comp.AltDisarm && TryComp<MeleeWeaponComponent>(entity, out var shoveComp))
        {
            ClientShove((entity, shoveComp), coordinates);
            return;
        }

        // primary attack
        if (useDown == BoundKeyState.Down)
        {
            ClientAttack(entity, coordinates, weapon.Value);
        }
    }

    protected override bool InRange(EntityUid user, EntityUid target, float range, ICommonSession? session)
    {
        var xform = Transform(target);
        var targetCoordinates = xform.Coordinates;
        var targetLocalAngle = xform.LocalRotation;

        return Interaction.InRangeUnobstructed(user, target, targetCoordinates, targetLocalAngle, range, overlapCheck: false);
    }

    protected override void DoDamageEffect(List<EntityUid> targets, EntityUid? user, TransformComponent targetXform)
    {
        // Server never sends the event to us for predictiveeevent.
        ColorFlashEffect.RaiseEffect(Color.Red, targets, Filter.Local());
    }

    /// <summary>
    /// Raises a heavy attack event with the relevant attacked entities.
    /// This is to avoid lag effecting the client's perspective too much.
    /// </summary>
    private void ClientAttack(EntityUid user, EntityCoordinates coordinates, Entity<MeleeWeaponComponent> weapon)
    {
        var targetMap = TransformSystem.ToMapCoordinates(coordinates);
        var target = GetClickedEntity(targetMap);

        // Don't light-attack if interaction will be handling this instead
        if (target.HasValue && Interaction.CombatModeCanHandInteract(user, target.Value))
            return;

        var userXform = Transform(user);

        if (targetMap.MapId != userXform.MapID)
            return;

        var userPos = TransformSystem.GetWorldPosition(userXform);

        // mimics server-side lagcomp
        var adjustedRange = weapon.Comp.Range + 0.1f;

        // Light attacks occur only if we're clicking a specific target in our approximate weapon range.
        // Heavy attacks occur in all other situations.
        if (target.HasValue &&
            (userPos - TransformSystem.GetWorldPosition(target.Value)).Length() <= adjustedRange)
        {
            RaisePredictiveEvent(new LightAttackEvent(GetNetEntity(target), GetNetEntity(weapon), GetNetCoordinates(coordinates)));
        }
        else
        {
            var direction = targetMap.Position - userPos;
            var distance = MathF.Min(weapon.Comp.Range, direction.Length());

            // This should really be improved. GetEntitiesInArc uses pos instead of bounding boxes.
            // Server will validate it with InRangeUnobstructed.
            var entities = GetNetEntityList(ArcRayCast(userPos, direction.ToWorldAngle(), weapon.Comp.Angle, distance, userXform.MapID, user).ToList());
            RaisePredictiveEvent(new HeavyAttackEvent(GetNetEntity(weapon), entities.GetRange(0, Math.Min(MaxTargets, entities.Count)), GetNetCoordinates(coordinates)));
        }
    }

    private void ClientShove(Entity<MeleeWeaponComponent> ent, EntityCoordinates coordinates)
    {
        var targetMap = TransformSystem.ToMapCoordinates(coordinates);
        var target = GetClickedEntity(targetMap);

        var userXform = Transform(ent);

        if (targetMap.MapId != userXform.MapID)
            return;

        var userPos = TransformSystem.GetWorldPosition(userXform);

        // mimics server-side lagcomp
        var adjustedRange = ent.Comp.Range + 0.1f;

        // Light attacks occur only if we're clicking a specific target in our approximate weapon range.
        // Heavy attacks occur in all other situations.
        if (target.HasValue &&
            (userPos - TransformSystem.GetWorldPosition(target.Value)).Length() <= adjustedRange)
        {
            RaisePredictiveEvent(new ShoveAttackEvent(GetNetEntity(target), GetNetCoordinates(coordinates)));
        }
        else
        {
            var direction = targetMap.Position - userPos;
            var distance = MathF.Min(ent.Comp.Range, direction.Length());

            // This should really be improved. GetEntitiesInArc uses pos instead of bounding boxes.
            // Server will validate it with InRangeUnobstructed.
            var entities = GetNetEntityList(ArcRayCast(userPos, direction.ToWorldAngle(), ent.Comp.ShoveAngle, distance, userXform.MapID, ent).ToList());
            RaisePredictiveEvent(new WideShoveAttackEvent(entities.GetRange(0, Math.Min(MaxTargets, entities.Count)), GetNetCoordinates(coordinates)));
        }
    }

    private void OnMeleeLunge(MeleeLungeEvent ev)
    {
        if (!TryGetEntity(ev.Entity, out var ent) ||
            !TryGetEntity(ev.Weapon, out var entWeapon))
            return;

        // Entity might not have been sent by PVS.
        if (Exists(ent) && Exists(entWeapon))
            DoLunge(ent.Value, entWeapon.Value, ev.Angle, ev.LocalPos, ev.Animation);
    }

    private EntityUid? GetClickedEntity(MapCoordinates mousePos)
    {
        if (_stateManager.CurrentState is GameplayStateBase screen)
            return screen.GetClickedEntity(mousePos);
        return null;
    }
}
