using System.Diagnostics.CodeAnalysis;
using Content.Server._ES.Spawning.Components;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Spawning;

public sealed class ESPersonalLockerSystem : EntitySystem
{
    [Dependency] private readonly LabelSystem _label = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AfterGeneralRecordCreatedEvent>(OnGeneralRecordCreated);
    }

    private void OnGeneralRecordCreated(AfterGeneralRecordCreatedEvent args)
    {
        AssignPersonalLocker(args.Key, args.Record.Name, args.Record.JobPrototype);
    }

    public bool AssignPersonalLocker(StationRecordKey key, string? name, ProtoId<JobPrototype> job)
    {
        if (!TryGetUnoccupiedPersonalLocker(job, out var locker))
            return false;

        _label.Label(locker.Value, name);

        if (TryComp<AccessReaderComponent>(locker, out var accessReader))
        {
            _accessReader.AddAccessKey((locker.Value, accessReader), key);
        }

        locker.Value.Comp.Assigned = true;
        return true;
    }

    public bool TryGetUnoccupiedPersonalLocker(ProtoId<JobPrototype> job, [NotNullWhen(true)] out Entity<ESPersonalLockerComponent>? locker)
    {
        var query = EntityQueryEnumerator<ESPersonalLockerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Job != job || comp.Assigned)
                continue;

            locker = (uid, comp);
            return true;
        }

        locker = null;
        return false;
    }
}
