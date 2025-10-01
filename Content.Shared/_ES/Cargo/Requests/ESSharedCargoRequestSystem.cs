using Content.Shared._ES.Cargo.Requests.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Station;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Cargo.Requests;

public abstract class ESSharedCargoRequestSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESCargoRequestStationComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ESCargoRequestConsoleComponent, MapInitEvent>(OnConsoleMapInit);
        SubscribeLocalEvent<ESCargoRequestConsoleComponent, ExaminedEvent>(OnExamined);

        Subs.BuiEvents<ESCargoRequestConsoleComponent>(ESCargoRequestConsoleUiKey.Key,
            subs =>
            {
                subs.Event<BoundUIOpenedEvent>(OnConsoleUiOpened);
                subs.Event<ESSetDepartmentIdMessage>(OnSetDepartmentId);
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
        if (!string.IsNullOrWhiteSpace(ent.Comp.ConsoleId))
            return;
        ent.Comp.ConsoleId = Loc.GetString(ent.Comp.DefaultConsoleId);
        Dirty(ent);
    }

    private void OnExamined(Entity<ESCargoRequestConsoleComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.UpdateIndicator)
            args.PushMarkup(Loc.GetString("es-cargo-request-examine-update"));
    }

    private void OnConsoleUiOpened(EntityUid uid, ESCargoRequestConsoleComponent component, BoundUIOpenedEvent args)
    {
        SetUpdateIndicator((uid, component), false);
    }

    protected virtual void OnSetDepartmentId(Entity<ESCargoRequestConsoleComponent> ent, ref ESSetDepartmentIdMessage args)
    {
        if (!ValidateDepartmentId(args.DepartmentId))
            return;

        var oldId = ent.Comp.ConsoleId;
        var newId = FormattedMessage.RemoveMarkupPermissive(args.DepartmentId);
        ent.Comp.ConsoleId = newId;
        Dirty(ent);
        _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.Actor):player} changed request console {ToPrettyString(ent)}\'s ID from {oldId} to {newId}");
    }

    private void OnCreateCargoRequest(Entity<ESCargoRequestConsoleComponent> ent, ref ESCreateCargoRequestMessage args)
    {
        if (ent.Comp.MasterConsole)
            return;

        var body = FormattedMessage.RemoveMarkupPermissive(args.Body);
        if (body.Length > ESCargoRequestConsoleComponent.MaxBodyLength)
            return;

        if (_station.GetOwningStation(ent) is not { } station ||
            !TryComp<ESCargoRequestStationComponent>(station, out var stationComp))
            return;

        var userName = Identity.Name(args.Actor, EntityManager);
        CreateRequest((station, stationComp), userName, ent.Comp.ConsoleId, body);
        SetRelevantUpdateIndicators(ent.Comp.ConsoleId, true);
        _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.Actor):player} added request RID#{stationComp.NextRequestId - 1} for \"{body}\"");
    }

    private void OnSetCargoRequestStatus(Entity<ESCargoRequestConsoleComponent> ent, ref ESSetCargoRequestStatusMessage args)
    {
        if (!ent.Comp.MasterConsole)
            return;

        if (_station.GetOwningStation(ent) is not { } station ||
            !TryComp<ESCargoRequestStationComponent>(station, out var stationComp) ||
            !stationComp.Requests.TryGetValue(args.RequestId, out var request))
            return;

        var oldStatus = request.Status;
        if (!TrySetRequestStatus((station, stationComp), args.RequestId, args.NewStatus))
            return;
        SetRelevantUpdateIndicators(request.Department, true);
        _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.Actor):player} set cargo status of RID#{args.RequestId} from {oldStatus} to {args.NewStatus}");
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

    public void SetRelevantUpdateIndicators(string department, bool val)
    {
        var query = EntityQueryEnumerator<ESCargoRequestConsoleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.ConsoleId != department && !comp.MasterConsole)
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
        Appearance.SetData(ent.Owner, ESCargoRequestConsoleVisuals.Update, val);
    }

    public static bool ValidateDepartmentId(string id)
    {
        var realId = FormattedMessage.RemoveMarkupPermissive(id);
        return realId.Length <= ESCargoRequestConsoleComponent.MaxIdLength && !string.IsNullOrWhiteSpace(realId);
    }

    public static LocId GetLocalizedText(ESCargoRequestStatus status)
    {
        return status switch
        {
            ESCargoRequestStatus.Pending => "es-cargo-request-status-pending",
            ESCargoRequestStatus.InProgress => "es-cargo-request-status-in-progress",
            ESCargoRequestStatus.Completed => "es-cargo-request-status-completed",
            ESCargoRequestStatus.Cancelled => "es-cargo-request-status-cancelled",
            ESCargoRequestStatus.Denied => "es-cargo-request-status-denied",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };
    }
}
