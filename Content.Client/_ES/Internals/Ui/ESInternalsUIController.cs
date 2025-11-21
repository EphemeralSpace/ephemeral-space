using System.Linq;
using Content.Client.Atmos.EntitySystems;
using Content.Client.Gameplay;
using Content.Client.Hands.Systems;
using Content.Client.Inventory;
using Content.Client.Popups;
using Content.Client.UserInterface.Controls;
using Content.Shared._ES.Internals;
using Content.Shared.Atmos.Components;
using Content.Shared.Body.Components;
using Content.Shared.Input;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;

namespace Content.Client._ES.Internals.Ui;

[UsedImplicitly]
public sealed class ESInternalsUIController : UIController, IOnStateChanged<GameplayState>
{
    [Dependency] private readonly IPlayerManager _player = default!;

    [UISystemDependency] private readonly GasTankSystem _gasTank = default!;
    [UISystemDependency] private readonly HandsSystem _hands = default!;
    [UISystemDependency] private readonly ClientInventorySystem _inventory = default!;
    [UISystemDependency] private readonly PopupSystem _popup = default!;

    private SimpleRadialMenu? _menu;

    public void OnStateEntered(GameplayState state)
    {
        _menu = new SimpleRadialMenu();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ESToggleInternals, new PointerInputCmdHandler(OnToggleInternalsPressed, outsidePrediction: true))
            .Register<ESInternalsUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        _menu = null;

        CommandBinds.Unregister<ESInternalsUIController>();
    }

    private bool OnToggleInternalsPressed(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        // Do not deal with input when they are picking shit out of the radial
        if (_menu?.IsOpen == true)
            return false;

        if (_player.LocalEntity is not { } player ||
            !EntityManager.TryGetComponent<InternalsComponent>(player, out var internals))
            return false;

        // If they can't connect to a tank, notify them!
        if (internals.BreathTools.Count == 0)
        {
            _popup.PopupPredictedCursor(Loc.GetString("internals-self-no-breath-tool"), player, PopupType.Medium);
            return true;
        }

        // If they have a connected tank already, prioritize turning it off first
        if (internals.GasTankEntity is { } connectedTank)
        {
            SendToggleMessage(connectedTank);
            return true;
        }

        // Find candidate tanks
        var tanks = new HashSet<(EntityUid Tank, string)>();
        var slotEnumerator = _inventory.GetSlotEnumerator(player);
        while (slotEnumerator.MoveNext(out var inventorySlot))
        {
            if (inventorySlot.ContainedEntity is not { } entity)
                continue;

            if (!EntityManager.TryGetComponent<GasTankComponent>(entity, out var gasTank) ||
                !_gasTank.CanConnectToInternals((entity, gasTank)))
                continue;

            _inventory.TryGetSlot(player, inventorySlot.ID, out var def);
            var identifier = def?.DisplayName.ToLowerInvariant() ?? string.Empty;
            tanks.Add((entity, identifier));
        }

        foreach (var handId in _hands.EnumerateHands(player))
        {
            if (!_hands.TryGetHeldItem(player, handId, out var held) ||
                !_hands.TryGetHand(player, handId, out var hand))
                continue;

            if (!EntityManager.TryGetComponent<GasTankComponent>(held, out var gasTank) ||
                !_gasTank.CanConnectToInternals((held.Value, gasTank)))
                continue;

            // TODO: this should be pulled out to a common place.
            var slotName = Loc.GetString("es-internals-ui-hand-fmt", ("location", hand.Value.Location.ToString().ToLowerInvariant()));
            tanks.Add((held.Value, slotName));
        }

        switch (tanks.Count)
        {
            case 0:
                _popup.PopupPredictedCursor(Loc.GetString("internals-self-no-tank"), player, PopupType.Medium);
                break;
            case 1:
                SendToggleMessage(tanks.First().Tank);
                break;
            case > 1:
                _menu?.OpenOverMouseScreenPosition();
                _menu?.SetButtons(GetButtons(tanks));
                break;
        }
        return true;
    }

    private IEnumerable<RadialMenuOptionBase> GetButtons(HashSet<(EntityUid, string)> set)
    {
        foreach (var (tank, slot) in set)
        {
            var metaData = EntityManager.GetComponent<MetaDataComponent>(tank);

            var option = new RadialMenuActionOption<EntityUid>(SendToggleMessage, tank)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(tank),
                ToolTip = Loc.GetString("es-internals-ui-name-fmt", ("name", metaData.EntityName), ("slot", slot)),
            };
            yield return option;
        }
    }

    private void SendToggleMessage(EntityUid tank)
    {
        var tankNEnt = EntityManager.GetNetEntity(tank);
        EntityManager.RaisePredictiveEvent(new ESToggleInternalsEvent(tankNEnt));
    }
}
