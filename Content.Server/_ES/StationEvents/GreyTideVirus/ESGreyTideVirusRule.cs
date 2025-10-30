using Content.Server._ES.StationEvents.GreyTideVirus.Components;
using Content.Server.Doors.Systems;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared._ES.Voting.Components;
using Content.Shared._ES.Voting.Results;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Lock;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.StationEvents.GreyTideVirus;

public sealed class ESGreyTideVirusRule : StationEventSystem<ESGreyTideVirusComponent>
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly DoorSystem _door = default!;
    [Dependency] private readonly LockSystem _lock = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESGreyTideVirusComponent, ESSynchronizedVotesCompletedEvent>(OnSynchronizedVotesCompleted);
        SubscribeLocalEvent<ESAccessGroupVoteComponent, ESGetVoteOptionsEvent>(OnGetVoteOptions);
    }

    private void OnSynchronizedVotesCompleted(Entity<ESGreyTideVirusComponent> ent, ref ESSynchronizedVotesCompletedEvent args)
    {
        if (!args.TryGetResult<ESAccessVoteOption>(0, out var access))
            return;

        ent.Comp.AccessGroup = access.Access;

        if (TryComp<StationEventComponent>(ent, out var stationEvent))
        {
            stationEvent.StartAnnouncement = Loc.GetString("es-station-event-greytide-virus-start-announcement",
                ("dept", Loc.GetString(PrototypeManager.Index(access.Access).GetAccessGroupName())));
        }
    }

    private void OnGetVoteOptions(Entity<ESAccessGroupVoteComponent> ent, ref ESGetVoteOptionsEvent args)
    {
        var options = new List<ProtoId<AccessGroupPrototype>>(ent.Comp.Options);
        var count = Math.Min(ent.Comp.Options.Count, ent.Comp.Count);
        for (var i = 0; i < count; i++)
        {
            args.Options.Add(new ESAccessVoteOption(PrototypeManager.Index(RobustRandom.PickAndTake(options))));
        }
    }

    protected override void Started(EntityUid uid, ESGreyTideVirusComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

        var accessGroup = PrototypeManager.Index(component.AccessGroup);

        var firelockQuery = GetEntityQuery<FirelockComponent>();

        // Unlock secure lockers
        var lockQuery = EntityQueryEnumerator<ESGreyTideVirusTargetComponent, LockComponent, AccessReaderComponent, TransformComponent>();
        while (lockQuery.MoveNext(out var lockUid,  out _, out var lockComp, out var accessComp, out var xform))
        {
            if (RobustRandom.Prob(component.IgnoreChance))
                continue;

            // make sure not to hit CentCom or other maps
            if (StationSystem.GetOwningStation(lockUid, xform) != chosenStation)
                continue;

            // check access
            // the AreAccessTagsAllowed function is a little weird because it technically has support for certain tags to be locked out of opening something
            // which might have unintended side effects (see the comments in the function itself)
            // but no one uses that yet, so it is fine for now
            if (!_accessReader.AreAccessTagsAllowed(accessGroup.Tags, accessComp) ||
                _accessReader.AreAccessTagsAllowed(component.Blacklist, accessComp))
                continue;

            _lock.Unlock(lockUid, null, lockComp);
        }

        // Bolt open doors
        var airlockQuery = EntityQueryEnumerator<ESGreyTideVirusTargetComponent, AirlockComponent, DoorComponent, TransformComponent>();
        while (airlockQuery.MoveNext(out var airlockUid, out _, out var airlockComp, out var doorComp, out var xform))
        {
            if (RobustRandom.Prob(component.IgnoreChance))
                continue;

            // don't space everything
            if (firelockQuery.HasComp(airlockUid))
                continue;

            // make sure not to hit CentCom or other maps
            if (StationSystem.GetOwningStation(airlockUid, xform) != chosenStation)
                continue;

            // use the access reader from the door electronics if they exist
            if (!_accessReader.GetMainAccessReader(airlockUid, out var accessEnt))
                continue;

            // check access
            if (!_accessReader.AreAccessTagsAllowed(accessGroup.Tags, accessEnt.Value.Comp) ||
                _accessReader.AreAccessTagsAllowed(component.Blacklist, accessEnt.Value.Comp))
                continue;

            _door.TryOpenAndBolt(airlockUid, doorComp, airlockComp);
        }
    }
}
