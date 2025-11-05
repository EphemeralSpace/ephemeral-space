using Content.Server.Nuke;
using Content.Shared._ES.Nuke;
using Content.Shared._ES.Nuke.Components;
using Content.Shared.Nuke;

namespace Content.Server._ES.Nuke;

/// <inheritdoc/>
public sealed class ESCryptoNukeSystem : ESSharedCryptoNukeSystem
{
    protected override void UpdateUiState(Entity<ESCryptoNukeConsoleComponent, UserInterfaceComponent> ent)
    {
        var state = new ESCryptoNukeConsoleBuiState();

        var diskQuery = EntityQueryEnumerator<NukeDiskComponent, TransformComponent>();
        while (diskQuery.MoveNext(out _, out _, out var xform))
        {
            state.DiskLocations.Add(GetNetCoordinates(xform.Coordinates));
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

            UpdateUiState((uid, tracker, ui));
        }
    }
}
