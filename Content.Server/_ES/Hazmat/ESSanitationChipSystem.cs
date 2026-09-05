using Content.Shared.Chemistry.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Timing;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Tag;
using Content.Shared.Popups;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Charges.Components;
using Content.Shared.Tools.Systems;
using Content.Shared._ES.Core.Timer;
using Content.Shared.Maps;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Administration.Logs;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Spreader;
using Content.Server.Power.EntitySystems;
using Content.Server.Atmos.Monitor.Components;
using Content.Server._ES.Announcements;
using Content.Server.Pinpointer;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;

using Content.Shared._ES.Hazmat.Components;
using Content.Shared._ES.Hazmat;

namespace Content.Server._ES.Hazmat;

public sealed partial class ESSanitationChipSystem : ESSharedSanitationChipSystem
{
    [Dependency] private MapSystem _map = default!;
    [Dependency] private SmokeSystem _smoke = default!;
    [Dependency] private SharedMapSystem _mapMan = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private SpreaderSystem _spreader = default!;
    [Dependency] private ESAnnouncementSystem _chat = default!;
    [Dependency] private ESEntityTimerSystem _timer = default!;
    [Dependency] private WeldableSystem _weldable = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private SharedChargesSystem _sharedCharges = default!;
    [Dependency] private PowerReceiverSystem _powerReceiverSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ESSanitationChipComponent, ESSanitationChipDoAfterEvent>(OnSanitationChipDoAfter);
    }

    private void OnSanitationChipDoAfter(Entity<ESSanitationChipComponent> chip, ref ESSanitationChipDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        args.Handled |= TryUseSanitationChip(chip, args.Args.User, args.Args.Target.Value);
    }

    private bool TryUseSanitationChip(Entity<ESSanitationChipComponent> chip, EntityUid user, EntityUid target)
    {
        if (!HasComp<AirAlarmComponent>(target))
            return false;

        var seconds = chip.Comp.TimeUntilGasSpawn.TotalSeconds.ToString();

        var location = FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString(target));

        _chat.DispatchRoundAnnouncement(Loc.GetString("es-sanitation-chip-announcement", ("seconds", seconds), ("area", location)),
            Loc.GetString("es-station-event-announcer"),
            announcementSound: new SoundPathSpecifier("/Audio/_ES/Announcements/attention_medium.ogg"),
            colorOverride: Color.LightGoldenrodYellow,
            important: true);

        var airAlarm = Comp<AirAlarmComponent>(target);

        var query = EntityQueryEnumerator<DeviceNetworkComponent>();

        var addresses = airAlarm.VentData.Keys;

        // change the sprite using the event before we spawn the gas
        while (query.MoveNext(out var uid, out var comp))
        {
            var checkAddress = comp.Address;
            if (!addresses.Contains(checkAddress))
                continue;

            if (_weldable.IsWelded(uid))
            {
                continue;
            }
            else if (!_powerReceiverSystem.IsPowered(uid) || !Comp<GasVentPumpComponent>(uid).Enabled)
            {
                continue;
            }

            var ev = new ESSanitationChipActivatedEvent();
            RaiseLocalEvent(uid, ref ev);
        }

        _ = _timer.SpawnMethodTimer(chip.Comp.TimeUntilGasSpawn, () => SpawnGas(chip, target));

        if (HasComp<LimitedChargesComponent>(chip.Owner))
        {
            var chargesComp = Comp<LimitedChargesComponent>(chip.Owner);
            _sharedCharges.TryUseCharge((chip.Owner, chargesComp));
        }

        return true;
    }

    private void SpawnGas(Entity<ESSanitationChipComponent> chip, EntityUid target)
    {
        var airAlarm = Comp<AirAlarmComponent>(target);

        var addresses = airAlarm.VentData.Keys;

        var query = EntityQueryEnumerator<DeviceNetworkComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            var checkAddress = comp.Address;
            if (!addresses.Contains(checkAddress))
                continue;

            if (_weldable.IsWelded(uid))
            {
                continue;
            }
            else if (!_powerReceiverSystem.IsPowered(uid) || !Comp<GasVentPumpComponent>(uid).Enabled)
            {
                continue;
            }
            var xform = CompOrNull<TransformComponent>(uid);
            if (xform == null)
                continue;

            // then spawn the foam out of those vents locations
            var mapCoords = _transform.GetMapCoordinates(uid, xform);
            if (!_mapMan.TryFindGridAt(mapCoords, out var gridUid, out var gridComp) ||
                !_map.TryGetTileRef(gridUid, gridComp, xform.Coordinates, out var tileRef) ||
                tileRef.Tile.IsEmpty)
            {
                continue;
            }

            if (_spreader.RequiresFloorToSpread(chip.Comp.SmokePrototype.ToString()) && _turf.IsSpace(tileRef))
                continue;

            var coords = _map.MapToGrid(gridUid, mapCoords);
            var smoke = Spawn(chip.Comp.SmokePrototype, coords.SnapToGrid());
            if (!TryComp<SmokeComponent>(smoke, out var smokeComp))
            {
                Log.Error($"Smoke prototype {chip.Comp.SmokePrototype} was missing SmokeComponent");
                Del(smoke);
                continue;
            }

            _smoke.StartSmoke(smoke, chip.Comp.Solution.Clone(), (float)chip.Comp.Duration.TotalSeconds, chip.Comp.SpreadAmount, smokeComp);
            _timer.SpawnMethodTimer(chip.Comp.Duration,
            () => {
                // this will remove the component
                var finishedEv = new ESSanitationChipFinishedEvent();
                RaiseLocalEvent(uid, ref finishedEv);
            });
        }
    }
}
