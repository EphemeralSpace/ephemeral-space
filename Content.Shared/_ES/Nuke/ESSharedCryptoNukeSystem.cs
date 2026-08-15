using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._ES.Nuke.Components;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Components;
using Content.Shared._ES.Sparks;
using Content.Shared._ES.Stagehand;
using Content.Shared._ES.WarpDrive;
using Content.Shared.Examine;
using Content.Shared.Mind;
using Content.Shared.Station;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._ES.Nuke;

public abstract partial class ESSharedCryptoNukeSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private ESSharedObjectiveSystem _objective = default!;
    [Dependency] private ESSparksSystem _sparks = default!;
    [Dependency] protected SharedStationSystem Station = default!;
    [Dependency] protected SharedUserInterfaceSystem UserInterface = default!;
    [Dependency] private ESSharedStagehandNotificationsSystem _notif = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESCryptoNukeConsoleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ESCryptoNukeConsoleComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<ESCryptoNukeHackObjectiveComponent, ESGetObjectiveProgressEvent>(OnGetProgress);

        Subs.BuiEvents<ESCryptoNukeConsoleComponent>(ESCryptoNukeConsoleUiKey.Key,
            subs =>
            {
                subs.Event<ESHackCryptoNukeConsoleBuiMessage>(OnHackMessage);
                subs.Event<ESSecurityOverrideCryptoNukeConsoleBuiMessage>(OnOverrideMessage);
            });
    }

    private void OnMapInit(Entity<ESCryptoNukeConsoleComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdateTime = Timing.CurTime + ent.Comp.NextUpdateTime * _random.NextFloat();
    }

    private void OnExamined(Entity<ESCryptoNukeConsoleComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.Compromised)
            return;
        args.PushMarkup(Loc.GetString("es-cryptonuke-examine-compromised"));
    }

    private void OnGetProgress(Entity<ESCryptoNukeHackObjectiveComponent> ent, ref ESGetObjectiveProgressEvent args)
    {
        args.Progress = GetCompromisedTerminalPercent();
    }

    private void OnHackMessage(Entity<ESCryptoNukeConsoleComponent> ent, ref ESHackCryptoNukeConsoleBuiMessage args)
    {
        if (ent.Comp.Compromised)
            return;

        if (!CanHack(args.Actor))
            return;

        _sparks.DoSparks(ent, user: args.Actor, cooldown: false);

        ent.Comp.Compromised = true;
        Dirty(ent);
        UpdateUiState((ent, ent, Comp<UserInterfaceComponent>(ent)));
        _notif.SendStagehandNotification(Loc.GetString("es-stagehand-notification-terminal-compromised",
            ("player", _notif.WrapEntityName(args.Actor)),
            ("terminal", ent.Owner)),
            ESStagehandNotificationSeverity.High);

        _objective.RefreshObjectiveProgress<ESCryptoNukeHackObjectiveComponent>();
    }

    private void OnOverrideMessage(Entity<ESCryptoNukeConsoleComponent> ent, ref ESSecurityOverrideCryptoNukeConsoleBuiMessage args)
    {
        if (!CanPotentiallyOverride(args.Actor))
            return;

        if (ent.Comp.WarpDriveSecurityOverridden || !ent.Comp.CanOverrideWarpDriveSecurity)
            return;

        _sparks.DoSparks(ent, user: args.Actor, cooldown: false);
        ent.Comp.WarpDriveSecurityOverridden = true;
        Dirty(ent);
        UpdateUiState((ent, ent, Comp<UserInterfaceComponent>(ent)));

        _notif.SendStagehandNotification(Loc.GetString("es-stagehand-notification-terminal-warp-drive-override",
            ("player", _notif.WrapEntityName(args.Actor)),
            ("terminal", ent.Owner))); // medium intentionally, since it plays an announcement anyway

        var ev = new ESCryptoNukeSecurityOverridenEvent(ent.Owner, args.Actor);
        RaiseLocalEvent(ref ev);
    }

    protected virtual void UpdateUiState(Entity<ESCryptoNukeConsoleComponent, UserInterfaceComponent> ent)
    {
    }

    /// <summary>
    /// Checks all consoles on a station to see if they are all compromised.
    /// </summary>
    public bool IsStationCompromised([NotNullWhen(true)] EntityUid? station)
    {
        if (station is null)
            return false;

        return MathHelper.CloseTo(GetCompromisedTerminalPercent(station), 1f);
    }

    public float GetCompromisedTerminalPercent(EntityUid? station = null)
    {
        var total = 0;
        var compromised = 0;

        var query = EntityQueryEnumerator<ESCryptoNukeConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (station != null)
            {
                if (Station.GetOwningStation(uid, xform) != station)
                    continue;
            }

            total += 1;
            // Exit early if we find a single compromised consoles.
            if (comp.Compromised)
                compromised += 1;
        }

        // If we have no consoles, assume that they're all destroyed.
        if (total == 0)
            return 1f;

        return (float) compromised / total;
    }

    /// <summary>
    /// Checks if an entity has the capability of hacking the cryptonuke console.
    /// </summary>
    public bool CanPotentiallyHack(EntityUid uid)
    {
        return _objective.HasObjectiveOfType<ESCryptoNukeHackObjectiveComponent>(_mind.GetMind(uid));
    }

    /// <summary>
    /// Checks if the entity is currently able to hack the cryptonuke console.
    /// </summary>
    public bool CanHack(EntityUid uid)
    {
        if (!_mind.TryGetMind(uid, out var mind))
            return false;

        if (!_objective.HasObjectiveOfType<ESCryptoNukeHackObjectiveComponent>(mind))
            return false;

        return _objective.GetObjectives<ESNukePrereqObjectiveComponent>(mind.Value.Owner)
            .All(e => _objective.IsCompleted(e.Owner));
    }

    public bool CanPotentiallyOverride(EntityUid uid)
    {
        if (!_mind.TryGetMind(uid, out var mind))
            return false;

        return _objective.HasObjectiveOfType<ESWarpDriveObjectiveComponent>(mind);
    }
}
