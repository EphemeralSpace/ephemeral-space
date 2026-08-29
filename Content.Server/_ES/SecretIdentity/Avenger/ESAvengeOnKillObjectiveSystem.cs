using Content.Server._ES.SecretIdentity.Avenger.Components;
using Content.Server.Actions;
using Content.Server.Pinpointer;
using Content.Server.Roles.Jobs;
using Content.Shared._ES.Chat;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.Objectives.Target;
using Content.Shared.Mind;
using Robust.Server.Player;
using Robust.Shared.Utility;

namespace Content.Server._ES.SecretIdentity.Avenger;

public sealed partial class ESAvengeOnKillObjectiveSystem : ESBaseTargetObjectiveSystem<ESAvengeOnKillObjectiveComponent>
{
    [Dependency] private IESSharedChatManager _chatManager = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private JobSystem _job = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private NavMapSystem _navMap = default!;

    public override Type[] TargetRelayComponents { get; } = [typeof(ESAvengeOnKillObjectiveMarkerComponent)];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESAvengeOnKillObjectiveMarkerComponent, ESPlayerKilledEvent>(OnKillReported);
    }

    private void OnKillReported(Entity<ESAvengeOnKillObjectiveMarkerComponent> ent, ref ESPlayerKilledEvent args)
    {
        foreach (var avenge in GetTargetingObjectives(ent))
        {
            AvengeKill(avenge, args);
        }
    }

    private void AvengeKill(Entity<ESAvengeOnKillObjectiveComponent> avenge, ESPlayerKilledEvent args)
    {
        if (!ObjectivesSys.TryFindObjectiveHolder(avenge.Owner, out var holder))
            return;

        var killerHasObjective = false;
        if (args.ValidKill && MindSys.TryGetMind(args.Killer.Value, out var killerMind, out _))
            killerHasObjective = ObjectivesSys.HasObjective(killerMind, avenge);

        var validKill = args.ValidKill && !killerHasObjective;

        if (TryComp<MindComponent>(holder, out var mind) &&
            _player.TryGetSessionById(mind.UserId, out var session))
        {
            var name = Name(args.Killed);
            var locationString = FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString(args.Killed));

            var locale = validKill ? "es-avenger-die-message-kill" : "es-avenger-die-message";

            var msg = Loc.GetString(locale, ("name", name), ("location", locationString));
            _chatManager.SendServerMessage(msg, session, Color.Red);
        }

        // Check for
        // - if the victim killed themselves
        // - if the victim died via the environment
        // - if the killer was actually the person who had to protect this person.
        if (!validKill)
            return;

        if (!ObjectivesSys.TryAddObjective(holder.Value.AsNullable(), avenge.Comp.AvengeObjective, out var objective))
            return;
        TargetObjective.SetTarget(objective.Value.Owner, args.Killer);
        _metaData.SetEntityName(objective.Value,
            Loc.GetString(avenge.Comp.AvengeTitle,
            ("targetName", Name(args.Killed)),
            ("job", _job.GetJobName(args.Killed))));

        if (mind?.OwnedEntity is { } body)
            _actions.AddAction(body, avenge.Comp.ActionPrototype);

        // Remove the protect objective
        ObjectivesSys.TryRemoveObjective(avenge.Owner);
    }
}
