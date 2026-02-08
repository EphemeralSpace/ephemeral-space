using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Gameplay;
using Content.Client.Hands.Systems;
using Content.Client.Inventory;
using Content.Client.Popups;
using Content.Client.UserInterface.Controls;
using Content.Shared._ES.Light;
using Content.Shared.Input;
using Content.Shared.Light.Components;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Utility;

namespace Content.Client._ES.Lighting.Ui;

/// <summary>
/// UI controller that handles toggling flashlights.
/// </summary>
[UsedImplicitly]
public sealed class ESFlashlightUIController : UIController, IOnStateChanged<GameplayState>
{
    [Dependency] private readonly IPlayerManager _player = default!;

    [UISystemDependency] private readonly HandsSystem _hands = default!;
    [UISystemDependency] private readonly ClientInventorySystem _inventory = default!;
    [UISystemDependency] private readonly PopupSystem _popup = default!;

    private SimpleRadialMenu? _menu;

    public void OnStateEntered(GameplayState state)
    {
        _menu = new SimpleRadialMenu();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ESToggleFlashlight, new PointerInputCmdHandler(OnToggleFlashlightPressed, outsidePrediction: true))
            .Register<ESFlashlightUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        _menu = null;

        CommandBinds.Unregister<ESFlashlightUIController>();
    }

    private bool OnToggleFlashlightPressed(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        // Do not deal with input when they are picking shit out of the radial
        if (_menu?.IsOpen == true)
            return false;

        if (_player.LocalEntity is not { } player)
            return false;

        if (TryGetFlashlight(out var light))
        {
            SendToggleMessage(light.Value);
        }
        else
        {
            _popup.PopupPredictedCursor(Loc.GetString("es-flashlight-popup-no-flashlight"), player, PopupType.Medium);
        }
        return true;
    }

    public bool TryGetFlashlight([NotNullWhen(true)] out EntityUid? outLight)
    {
        outLight = null;
        if (_player.LocalEntity is not { } player)
            return false;

        // Find candidate lights
        var lights = new HashSet<(EntityUid Uid, bool On)>();
        var slotEnumerator = _inventory.GetSlotEnumerator(player);
        while (slotEnumerator.MoveNext(out var inventorySlot))
        {
            if (inventorySlot.ContainedEntity is not { } entity)
                continue;

            // CHECK BOTH FLASHLIGHTS, FOR SOME REASON
            if (EntityManager.TryGetComponent<HandheldLightComponent>(entity, out var handheld))
            {
                lights.Add((entity, handheld.Activated));
            }
            else if (EntityManager.TryGetComponent<UnpoweredFlashlightComponent>(entity, out var flashlight))
            {
                lights.Add((entity, flashlight.LightOn));
            }
        }

        foreach (var handId in _hands.EnumerateHands(player))
        {
            if (!_hands.TryGetHeldItem(player, handId, out var held))
                continue;

            // CHECK BOTH FLASHLIGHTS, FOR SOME REASON
            if (EntityManager.TryGetComponent<HandheldLightComponent>(held, out var handheld))
            {
                lights.Add((held.Value, handheld.Activated));
            }
            else if (EntityManager.TryGetComponent<UnpoweredFlashlightComponent>(held, out var flashlight))
            {
                lights.Add((held.Value, flashlight.LightOn));
            }
        }

        {
            if (EntityManager.TryGetComponent<HandheldLightComponent>(player, out var handheld))
            {
                lights.Add((player, handheld.Activated));
            }
            else if (EntityManager.TryGetComponent<UnpoweredFlashlightComponent>(player, out var flashlight))
            {
                lights.Add((player, flashlight.LightOn));
            }
        }

        if (lights.Count == 0)
            return false;

        if (lights.FirstOrNull(pair => pair.On) is { } light)
        {
            outLight = light.Uid;
            return true;
        }

        outLight = lights.MaxBy(pair => EntityManager.GetComponentOrNull<PointLightComponent>(pair.Uid)?.Radius ?? 0).Uid;
        return true;
    }

    private void SendToggleMessage(EntityUid uid)
    {
        var netEnt = EntityManager.GetNetEntity(uid);
        EntityManager.RaisePredictiveEvent(new ESToggleFlashlightEvent(netEnt));
    }
}
