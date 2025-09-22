using Content.Shared._ES.Cargo.Requests.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Station;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Cargo.Requests;

public abstract class ESSharedCargoRequestSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESCargoRequestStationComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ESCargoRequestConsoleComponent, MapInitEvent>(OnConsoleMapInit);

        Subs.BuiEvents<ESCargoRequestConsoleComponent>(ESCargoRequestConsoleUiKey.Key,
            subs =>
            {
                subs.Event<BoundUIOpenedEvent>(OnConsoleUiOpened);
                subs.Event<ESCreateCargoRequestMessage>(OnCreateCargoRequest);
                subs.Event<ESSetCargoRequestStatusMessage>(OnSetCargoRequestStatus);
            });
    }

    private void OnMapInit(Entity<ESCargoRequestStationComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextRequestId = _random.Next(50, 150);
        Dirty(ent);
    }

    private void OnConsoleMapInit(Entity<ESCargoRequestConsoleComponent> ent, ref MapInitEvent args)
    {
        if (!string.IsNullOrWhiteSpace(ent.Comp.DepartmentString))
            return;
        ent.Comp.DepartmentString = Loc.GetString("es-cargo-request-console-dept-default");
        Dirty(ent);
    }

    private void OnConsoleUiOpened(EntityUid uid, ESCargoRequestConsoleComponent component, BoundUIOpenedEvent args)
    {
        SetUpdateIndicator((uid, component), false);
    }

    private void OnCreateCargoRequest(Entity<ESCargoRequestConsoleComponent> ent, ref ESCreateCargoRequestMessage args)
    {
        if (!ent.Comp.SettableStatuses.HasFlag(ESCargoRequestStatus.Pending))
            return;

        var body = FormattedMessage.RemoveMarkupPermissive(args.Body);
        if (body.Length > ESCargoRequestConsoleComponent.MaxBodyLength)
            return;

        if (_station.GetOwningStation(ent) is not { } station ||
            !TryComp<ESCargoRequestStationComponent>(station, out var stationComp))
            return;

        var userName = Identity.Name(args.Actor, EntityManager);
        CreateRequest((station, stationComp), userName, ent.Comp.DepartmentString, body);
        SetRelevantUpdateIndicators(ent.Comp.DepartmentString, true);
        // LOG
    }

    private void OnSetCargoRequestStatus(Entity<ESCargoRequestConsoleComponent> ent, ref ESSetCargoRequestStatusMessage args)
    {
        if (!CanSetStatus(ent, args.NewStatus))
            return;

        if (_station.GetOwningStation(ent) is not { } station ||
            !TryComp<ESCargoRequestStationComponent>(station, out var stationComp) ||
            !stationComp.Requests.TryGetValue(args.RequestId, out var request))
            return;

        if (!TrySetRequestStatus((station, stationComp), args.RequestId, args.NewStatus))
            return;
        SetRelevantUpdateIndicators(request.Department, true);
        // LOG
    }

    public ESCargoRequest CreateRequest(Entity<ESCargoRequestStationComponent> ent, string user, string department, string requestBody)
    {
        var id = ent.Comp.NextRequestId++;
        var req = new ESCargoRequest
        {
            User = user,
            Department = department,
            RequestBody = requestBody,
            Status = ESCargoRequestStatus.Pending,
        };
        ent.Comp.Requests.Add(id, req);
        Dirty(ent);
        return req;
    }

    public bool TrySetRequestStatus(Entity<ESCargoRequestStationComponent> ent, int requestId, ESCargoRequestStatus status)
    {
        if (!ent.Comp.Requests.TryGetValue(requestId, out var request))
            return false;

        request.Status = status;
        Dirty(ent);
        return true;
    }

    public bool CanSetStatus(Entity<ESCargoRequestConsoleComponent> ent, ESCargoRequestStatus status)
    {
        return ent.Comp.SettableStatuses.HasFlag(status);
    }

    public void SetRelevantUpdateIndicators(string department, bool val)
    {
        var query = EntityQueryEnumerator<ESCargoRequestConsoleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.DepartmentString != department || comp.MasterConsole)
                continue;
            SetUpdateIndicator((uid, comp), val);
        }
    }

    public void SetUpdateIndicator(Entity<ESCargoRequestConsoleComponent> ent, bool val)
    {
        if (ent.Comp.UpdateIndicator == val)
            return;

        if (val && _userInterface.IsUiOpen(ent.Owner, ESCargoRequestConsoleUiKey.Key))
            return;

        ent.Comp.UpdateIndicator = val;
        _appearance.SetData(ent.Owner, ESCargoRequestConsoleVisuals.Update, val);
    }

    public static LocId GetLocalizedText(ESCargoRequestStatus status)
    {
        return status switch
        {
            ESCargoRequestStatus.Pending => "es-cargo-request-status-pending",
            ESCargoRequestStatus.InProgress => "es-cargo-request-status-in-progress",
            ESCargoRequestStatus.Completed => "es-cargo-request-status-completed",
            ESCargoRequestStatus.Cancelled => "es-cargo-request-status-cancelled",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };
    }
}
