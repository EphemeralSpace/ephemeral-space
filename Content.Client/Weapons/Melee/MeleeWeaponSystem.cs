using System.Linq;
using Content.Client.Gameplay;
using Content.Shared.Effects;
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
    [Dependency] private SharedColorFlashEffectSystem _color = default!;
    [Dependency] private MapSystem _map = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private EntityQuery<TransformComponent> _xformQuery;

    private const string MeleeLungeKey = "melee-lunge";

    public override void Initialize()
    {
        base.Initialize();
        _xformQuery = GetEntityQuery<TransformComponent>();
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

        var entityNull = _player.LocalEntity;

        if (entityNull == null)
            return;

        var entity = entityNull.Value;

        if (!TryGetWeapon(entity, out var weaponUid, out var weapon))
            return;

        if (!CombatMode.IsInCombatMode(entity) || !Blocker.CanAttack(entity, weapon: (weaponUid, weapon)))
        {
            weapon.Attacking = false;
            return;
        }

        var useDown = _inputSystem.CmdStates.GetState(EngineKeyFunctions.Use);
        var altDown = _inputSystem.CmdStates.GetState(EngineKeyFunctions.UseSecondary);

        if (weapon.AutoAttack || useDown != BoundKeyState.Down && altDown != BoundKeyState.Down)
        {
            if (weapon.Attacking)
            {
                RaisePredictiveEvent(new StopAttackEvent(GetNetEntity(weaponUid)));
            }
        }

        if (weapon.Attacking || weapon.NextAttack > Timing.CurTime)
        {
            return;
        }

        // TODO using targeted actions while combat mode is enabled should NOT trigger attacks.

        var mousePos = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);

        if (mousePos.MapId == MapId.Nullspace)
        {
            return;
        }

        EntityCoordinates coordinates;

        if (MapManager.TryFindGridAt(mousePos, out var gridUid, out _))
        {
            coordinates = TransformSystem.ToCoordinates(gridUid, mousePos);
        }
        else
        {
            coordinates = TransformSystem.ToCoordinates(_map.GetMap(mousePos.MapId), mousePos);
        }

        // secondary attack
        if (altDown == BoundKeyState.Down && weapon.AltDisarm)
        {
            ClientShove(mousePos, coordinates);
            return;
        }

        // primary attack
        if (useDown == BoundKeyState.Down)
        {
            EntityUid? target = null;
            if (_stateManager.CurrentState is GameplayStateBase screen)
                target = screen.GetClickedEntity(mousePos);

            var attackerPos = TransformSystem.GetMapCoordinates(weaponUid);

            // mimics server-side lagcomp
            var adjustedRange = weapon.Range + 0.1f;

            // Light attacks occur only if we're clicking a specific target in our approximate weapon range.
            // Heavy attacks occur in all other situations.
            if (target.HasValue
                && mousePos.MapId == attackerPos.MapId
                && (attackerPos.Position - TransformSystem.GetWorldPosition(target.Value)).Length() <= adjustedRange)
            {
                ClientLightAttack(entity, target.Value, coordinates, weaponUid, weapon);
            }
            else
            {
                ClientHeavyAttack(entity, coordinates, weaponUid, weapon);
            }
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
        _color.RaiseEffect(Color.Red, targets, Filter.Local());
    }

    /// <summary>
    /// Raises a heavy attack event with the relevant attacked entities.
    /// This is to avoid lag effecting the client's perspective too much.
    /// </summary>
    private void ClientHeavyAttack(EntityUid user, EntityCoordinates coordinates, EntityUid meleeUid, MeleeWeaponComponent component)
    {
        var targetMap = TransformSystem.ToMapCoordinates(coordinates);

        EntityUid? target = null;
        if (_stateManager.CurrentState is GameplayStateBase screen)
            target = screen.GetClickedEntity(targetMap);

        // Don't light-attack if interaction will be handling this instead
        if (target.HasValue && Interaction.CombatModeCanHandInteract(user, target.Value))
            return;

        // Only run on first prediction to avoid the potential raycast entities changing.
        if (!_xformQuery.TryGetComponent(user, out var userXform) ||
            !Timing.IsFirstTimePredicted)
        {
            return;
        }

        if (targetMap.MapId != userXform.MapID)
            return;

        var userPos = TransformSystem.GetWorldPosition(userXform);
        var direction = targetMap.Position - userPos;
        var distance = MathF.Min(component.Range, direction.Length());

        // This should really be improved. GetEntitiesInArc uses pos instead of bounding boxes.
        // Server will validate it with InRangeUnobstructed.
        var entities = GetNetEntityList(ArcRayCast(userPos, direction.ToWorldAngle(), component.Angle, distance, userXform.MapID, user).ToList());
        RaisePredictiveEvent(new HeavyAttackEvent(GetNetEntity(meleeUid), entities.GetRange(0, Math.Min(MaxTargets, entities.Count)), GetNetCoordinates(coordinates)));
    }

    private void ClientDisarm(EntityUid attacker, MapCoordinates mousePos, EntityCoordinates coordinates)
    {
        EntityUid? target = null;

        if (_stateManager.CurrentState is GameplayStateBase screen)
            target = screen.GetClickedEntity(mousePos);

        RaisePredictiveEvent(new DisarmAttackEvent(GetNetEntity(target), GetNetCoordinates(coordinates)));
    }

    private void ClientShove(MapCoordinates mousePos, EntityCoordinates coordinates)
    {
        EntityUid? target = null;

        if (_stateManager.CurrentState is GameplayStateBase screen)
            target = screen.GetClickedEntity(mousePos);

        RaisePredictiveEvent(new ShoveAttackEvent(GetNetEntity(target), GetNetCoordinates(coordinates)));
    }

    private void ClientLightAttack(EntityUid attacker, EntityUid target, EntityCoordinates coordinates, EntityUid weaponUid, MeleeWeaponComponent meleeComponent)
    {
        // Don't light-attack if interaction will be handling this instead
        if (Interaction.CombatModeCanHandInteract(attacker, target))
            return;

        RaisePredictiveEvent(new LightAttackEvent(GetNetEntity(target), GetNetEntity(weaponUid), GetNetCoordinates(coordinates)));
    }

    private void OnMeleeLunge(MeleeLungeEvent ev)
    {
        var ent = GetEntity(ev.Entity);
        var entWeapon = GetEntity(ev.Weapon);

        // Entity might not have been sent by PVS.
        if (Exists(ent) && Exists(entWeapon))
            DoLunge(ent, entWeapon, ev.Angle, ev.LocalPos, ev.Animation);
    }
}
