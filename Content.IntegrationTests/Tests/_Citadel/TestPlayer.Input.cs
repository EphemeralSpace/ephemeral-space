#nullable enable
using Content.Client.Weapons.Melee;
using Content.IntegrationTests.Tests._Citadel.Attributes;
using Content.Server.CombatMode;
using Content.Shared.CombatMode;
using Content.Shared.Weapons.Melee;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests._Citadel;

public sealed partial class TestPlayer
{
    [SidedDependency(Side.Client)] private readonly IInputManager _inputManager = default!;
    [SidedDependency(Side.Client)] private readonly IGameTiming _clientTiming = default!;
    [SidedDependency(Side.Server)] private readonly IGameTiming _serverTiming = default!;
    [SidedDependency(Side.Client)] private readonly IEntityManager _clientEntMan = default!;
    [System(Side.Client)] private readonly InputSystem _clientInputSys = default!;
    [System(Side.Server)] private readonly CombatModeSystem _serverCombatMode = default!;
    [System(Side.Server)] private readonly Robust.Server.GameObjects.TransformSystem _serverXformSys = default!;
    [System(Side.Server)] private readonly SharedMeleeWeaponSystem _serverWeaponSystem = default!;
    [System(Side.Client)] private readonly MeleeWeaponSystem _clientWeaponSystem = default!;

    /// <summary>
    ///     Make the client press and then release a key. This assumes the key is currently released.
    /// </summary>
    public async Task PressKey(
        BoundKeyFunction key,
        EntityCoordinates clientCoordinates,
        int ticks = 1,
        EntityUid? clientCursorEntity = null)
    {
        await SetKey(key, BoundKeyState.Down, clientCoordinates, clientCursorEntity);
        await _test.RunTicksSync(ticks);
        await SetKey(key, BoundKeyState.Up, clientCoordinates, clientCursorEntity);
        await _test.RunTicksSync(1);
    }

    /// <summary>
    ///     Make the client press or release a key.
    /// </summary>
    public async Task SetKey(
        BoundKeyFunction key,
        BoundKeyState state,
        EntityCoordinates clientCoordinates,
        EntityUid? clientCursorEntity = null,
        ScreenCoordinates? screenCoordinates = null)
    {
        var target = clientCursorEntity ?? default;
        var screen = screenCoordinates ?? default;

        var funcId = _inputManager.NetworkBindMap.KeyFunctionID(key);
        var message = new ClientFullInputCmdMessage(_clientTiming.CurTick, _clientTiming.TickFraction, funcId)
        {
            State = state,
            Coordinates = clientCoordinates,
            ScreenCoordinates = screen,
            Uid = target,
        };

        await _client.WaitPost(() => _clientInputSys.HandleInputCommand(_client.Session, key, message));
    }

    /// <summary>
    ///     Variant of <see cref="SetKey"/> for setting movement keys.
    /// </summary>
    public async Task SetMovementKey(DirectionFlag dir, BoundKeyState state)
    {
        if ((dir & DirectionFlag.South) != 0)
            await SetKey(EngineKeyFunctions.MoveDown, state, EntityCoordinates.Invalid);

        if ((dir & DirectionFlag.East) != 0)
            await SetKey(EngineKeyFunctions.MoveRight, state, EntityCoordinates.Invalid);

        if ((dir & DirectionFlag.North) != 0)
            await SetKey(EngineKeyFunctions.MoveUp, state, EntityCoordinates.Invalid);

        if ((dir & DirectionFlag.West) != 0)
            await SetKey(EngineKeyFunctions.MoveLeft, state, EntityCoordinates.Invalid);
    }

    /// <summary>
    ///     Make the client hold the move key in some direction for some amount of time.
    /// </summary>
    public async Task Move(DirectionFlag dir, float seconds)
    {
        await SetMovementKey(dir, BoundKeyState.Down);
        await _test.RunSeconds(seconds);
        await SetMovementKey(dir, BoundKeyState.Up);
        await _test.RunTicksSync(1);
    }

    /// <summary>
    ///     Sets the player's combat mode state.
    /// </summary>
    /// <param name="enabled">What state to set combat mode to.</param>
    public async Task SetCombatMode(bool enabled)
    {
        if (!_test.SEntMan.TryGetComponent(SEntity, out CombatModeComponent? combat))
        {
            throw new Exception($"Entity {_test.SEntMan.ToPrettyString(SEntity)} does not have a CombatModeComponent");
        }

        await _server.WaitPost(() => _serverCombatMode.SetInCombatMode(SEntity, enabled, combat));
        await _test.RunUntilSynced();

        Assert.That(combat.IsInCombatMode, Is.EqualTo(enabled), $"Player could not set combat mode to {enabled}");
    }

    /// <summary>
    ///     Causes the player to punch the given target with a light blow (instantaneous use press).
    /// </summary>
    /// <param name="target">The entity to assault.</param>
    /// <param name="waitOutCooldown">Whether this should automatically wait out the cooldown on melee.</param>
    public async Task Punch(EntityUid target, bool waitOutCooldown = false)
    {
        var clientEnt = _test.ToClientUid(target);

        var xform = _test.CComp<TransformComponent>(clientEnt);

        await SetCombatMode(true);

        await _server.WaitPost(() =>
        {
            if (_serverHandsSys.GetActiveItem(SEntity) is not { } item)
            {
                // Nothing in hand, try ourselves.
                item = SEntity;
            }

            _serverWeaponSystem.AttemptLightAttack(SEntity, item, _test.SComp<MeleeWeaponComponent>(item), target);
        });

        await SetCombatMode(false);

        if (waitOutCooldown)
        {
            await _test.RunTicksSync(1);
            var timeToWait = 0.0f;

            await _server.WaitPost(() =>
            {
                if (_test.SDeleted(SEntity))
                    return; // We got eviscerated.

                if (_serverHandsSys.GetActiveItem(SEntity) is not { } item)
                {
                    // Nothing in hand, try ourselves.
                    item = SEntity;
                }

                if (!_test.STryComp(item, out MeleeWeaponComponent? weapon))
                    return; // Not a weapon.

                var rate = _serverWeaponSystem.GetAttackRate(item, SEntity, weapon);

                // Small leeway to avoid nonsense
                timeToWait = (1f / rate) + 0.1f;
            });

            await _test.RunSeconds(timeToWait);
        }
    }
}
