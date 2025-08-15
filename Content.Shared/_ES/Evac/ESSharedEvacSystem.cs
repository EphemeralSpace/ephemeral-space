using Content.Shared._ES.CCVar;
using Content.Shared._ES.Evac.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Station;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Shared._ES.Evac;

public abstract class ESSharedEvacSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public float EvacVotePercentage { get; protected set; }

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESEvacStationComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ESEvacStationComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<ESEvacConsoleComponent, ComponentStartup>(OnConsoleStartup);
        SubscribeLocalEvent<ESEvacConsoleComponent, ComponentShutdown>(OnConsoleShutdown);
        SubscribeLocalEvent<ESEvacConsoleComponent, GridUidChangedEvent>(OnConsoleGridChanged);

        Subs.BuiEvents<ESEvacConsoleComponent>(ESEvacUiKey.Key,
            subs =>
            {
                subs.Event<ESToggleStationEvacMessage>(ToggleStationEvac);
            });

        Subs.CVar(_config, ESCVars.ESEvacVotePercentage, val => EvacVotePercentage = val, true);
    }

    private void OnMapInit(Entity<ESEvacStationComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextToggleTime = Timing.CurTime + ent.Comp.RoundstartDelay;
        Dirty(ent);
    }

    private void OnShutdown(Entity<ESEvacStationComponent> ent, ref ComponentShutdown args)
    {
        UpdateEvacVoteStatus();
    }

    private void OnConsoleStartup(Entity<ESEvacConsoleComponent> ent, ref ComponentStartup args)
    {
        _metaData.SetFlag(ent.Owner, MetaDataFlags.ExtraTransformEvents, true);
    }

    private void OnConsoleShutdown(Entity<ESEvacConsoleComponent> ent, ref ComponentShutdown args)
    {
        if (_station.GetOwningStation(ent) is not { } station ||
            !TryComp<ESEvacStationComponent>(station, out var evac))
            return;

        CheckEvacConsoles((station, evac));
    }

    private void OnConsoleGridChanged(Entity<ESEvacConsoleComponent> ent, ref GridUidChangedEvent args)
    {
        if (_station.GetOwningStation(args.OldGrid) is not { } station ||
            !TryComp<ESEvacStationComponent>(station, out var evac))
            return;

        CheckEvacConsoles((station, evac));
    }

    private void ToggleStationEvac(Entity<ESEvacConsoleComponent> ent, ref ESToggleStationEvacMessage args)
    {
        if (_station.GetOwningStation(ent) is not { } station ||
            !TryComp<ESEvacStationComponent>(station, out var comp))
            return;

        if (comp.Locked)
            return;

        if (Timing.CurTime < comp.NextToggleTime)
            return;

        _adminLog.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(args.Actor):user} set the station \"{ToPrettyString(ent.Owner)}\" evac beacon value to {!comp.EvacVoteEnabled}.");

        SetEvacVote((station, comp), !comp.EvacVoteEnabled);
    }

    private void CheckEvacConsoles(Entity<ESEvacStationComponent> ent)
    {
        // Don't run logic if we're already leaving.
        if (ent.Comp.EvacVoteEnabled)
            return;

        var any = false;
        var query = EntityQueryEnumerator<ESEvacConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (_station.GetOwningStation(uid, xform) != ent)
                continue;
            any = true;
            break;
        }

        // If one of the consoles is destroyed and none are left, we explicitly call evac.
        if (!any)
        {
            _adminLog.Add(LogType.Action, LogImpact.High, $"The station \"{ToPrettyString(ent.Owner)}\" evac beacon enabled due to having no consoles.");

            SetEvacVote(ent, true, Loc.GetString("es-evac-announcement-signal-console-destroyed"));
        }
    }

    public virtual void SetEvacVote(Entity<ESEvacStationComponent> ent, bool value, string? overrideMessage = null)
    {
        if (ent.Comp.EvacVoteEnabled == value)
            return;
        ent.Comp.EvacVoteEnabled = value;
        ent.Comp.NextToggleTime = Timing.CurTime + ent.Comp.ToggleDelay;
        Dirty(ent);
        UpdateEvacVoteStatus();
    }

    protected virtual void UpdateEvacVoteStatus()
    {

    }

}
