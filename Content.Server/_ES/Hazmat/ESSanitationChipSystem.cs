using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Content.Shared.Timing;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Tag;
using Content.Shared.Popups;
using Content.Shared.DeviceNetwork.Components;
using Robust.Shared.Prototypes;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Spreader;
using Content.Shared.Chemistry.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Maps;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Effects;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

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
        if (!HasComp<DeviceNetworkComponent>(target))
            return false;

        var deviceNetworkComp = Comp<DeviceNetworkComponent>(target);

        var address = deviceNetworkComp.Address;

        var query = EntityQueryEnumerator<DeviceNetworkComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            var checkAddress = comp.Address;
            if (!checkAddress.Equals(address))
                continue;

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

            _smoke.StartSmoke(smoke, chip.Comp.Solution, (float)chip.Comp.Duration.TotalSeconds, chip.Comp.SpreadAmount, smokeComp);
        }

        return true;
    }
}
