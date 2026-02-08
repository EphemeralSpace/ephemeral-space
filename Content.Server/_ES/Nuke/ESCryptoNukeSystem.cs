using System.Numerics;
using Content.Server.Nuke;
using Content.Shared._ES.Nuke;
using Content.Shared._ES.Nuke.Components;
using Content.Shared.Nuke;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server._ES.Nuke;

/// <inheritdoc/>
public sealed class ESCryptoNukeSystem : ESSharedCryptoNukeSystem
{
    [Dependency] private readonly TransformSystem _transform = default!;

    protected override void UpdateUiState(Entity<ESCryptoNukeConsoleComponent, UserInterfaceComponent> ent)
    {
        var state = new ESCryptoNukeConsoleBuiState();

        var diskQuery = EntityQueryEnumerator<NukeDiskComponent, TransformComponent>();
        while (diskQuery.MoveNext(out _, out _, out var xform))
        {
            EntityCoordinates coordinates;
            if (xform.GridUid != null)
            {
                coordinates = new EntityCoordinates(xform.GridUid.Value,
                    Vector2.Transform(_transform.GetWorldPosition(xform),
                        _transform.GetInvWorldMatrix(xform.GridUid.Value)));
            }
            else if (xform.MapUid != null)
            {
                coordinates = new EntityCoordinates(xform.MapUid.Value, _transform.GetWorldPosition(xform));
            }
            else
            {
                coordinates = EntityCoordinates.Invalid;
            }
            state.DiskLocations.Add(GetNetCoordinates(coordinates));
        }

        var station = Station.GetOwningStation(ent);
        if (IsStationCompromised(station))
        {
            var nukeQuery = EntityQueryEnumerator<NukeComponent>();
            while (nukeQuery.MoveNext(out _, out var nukeComp))
            {
                if (nukeComp.OriginStation == station)
                    state.Codes.Add(nukeComp.Code);
            }
        }

        state.CanHack = ArePreRequisiteObjectivesDone();

        UserInterface.SetUiState((ent, ent), ESCryptoNukeConsoleUiKey.Key, state);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ESCryptoNukeConsoleComponent, UserInterfaceComponent>();
        while (query.MoveNext(out var uid, out var tracker, out var ui))
        {
            if (Timing.CurTime < tracker.NextUpdateTime)
                continue;
            tracker.NextUpdateTime += tracker.UpdateRate;

            if (!UserInterface.IsUiOpen((uid, ui), ESCryptoNukeConsoleUiKey.Key))
                continue;

            UpdateUiState((uid, tracker, ui));
        }
    }
}
