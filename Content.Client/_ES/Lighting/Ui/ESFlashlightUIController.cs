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
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;

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

        // Find candidate tanks
        var lights = new HashSet<(EntityUid Uid, string?)>();
        var slotEnumerator = _inventory.GetSlotEnumerator(player);
        while (slotEnumerator.MoveNext(out var inventorySlot))
        {
            if (inventorySlot.ContainedEntity is not { } entity)
                continue;

            // CHECK BOTH FLASHLIGHTS, FOR SOME REASON
            if (!EntityManager.HasComponent<HandheldLightComponent>(entity) &&
                !EntityManager.HasComponent<UnpoweredFlashlightComponent>(entity))
                continue;

            _inventory.TryGetSlot(player, inventorySlot.ID, out var def);
            var identifier = def?.DisplayName.ToLowerInvariant() ?? string.Empty;
            lights.Add((entity, identifier));
        }

        foreach (var handId in _hands.EnumerateHands(player))
        {
            if (!_hands.TryGetHeldItem(player, handId, out var held) ||
                !_hands.TryGetHand(player, handId, out var hand))
                continue;

            // CHECK BOTH FLASHLIGHTS, FOR SOME REASON
            if (!EntityManager.HasComponent<HandheldLightComponent>(held) &&
                !EntityManager.HasComponent<UnpoweredFlashlightComponent>(held))
                continue;

            // See comment in ESInternalsUIController
            var slotName = Loc.GetString("es-internals-ui-hand-fmt", ("location", hand.Value.Location.ToString().ToLowerInvariant()));
            lights.Add((held.Value, slotName));
        }

        if (EntityManager.HasComponent<HandheldLightComponent>(player) ||
            EntityManager.HasComponent<UnpoweredFlashlightComponent>(player))
        {
            lights.Add((player, null));
        }

        switch (lights.Count)
        {
            case 0:
                _popup.PopupPredictedCursor(Loc.GetString("es-flashlight-popup-no-flashlight"), player, PopupType.Medium);
                break;
            case 1:
                SendToggleMessage(lights.First().Uid);
                break;
            case > 1:
                _menu?.OpenOverMouseScreenPosition();
                _menu?.SetButtons(GetButtons(lights));
                break;
        }
        return true;
    }

    private IEnumerable<RadialMenuOptionBase> GetButtons(HashSet<(EntityUid, string?)> set)
    {
        foreach (var (uid, slot) in set)
        {
            var metaData = EntityManager.GetComponent<MetaDataComponent>(uid);
            var tooltip = slot != null
                ? Loc.GetString("es-internals-ui-name-fmt", ("name", metaData.EntityName), ("slot", slot))
                : metaData.EntityName;
            var option = new RadialMenuActionOption<EntityUid>(SendToggleMessage, uid)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(uid),
                ToolTip = tooltip,
            };
            yield return option;
        }
    }

    private void SendToggleMessage(EntityUid uid)
    {
        var netEnt = EntityManager.GetNetEntity(uid);
        EntityManager.RaisePredictiveEvent(new ESToggleFlashlightEvent(netEnt));
    }
}
