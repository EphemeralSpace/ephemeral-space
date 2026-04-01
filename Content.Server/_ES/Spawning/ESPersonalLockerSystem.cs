using System.Diagnostics.CodeAnalysis;
using Content.Server._ES.Spawning.Components;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Spawning;

public sealed class ESPersonalLockerSystem : EntitySystem
{
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AfterGeneralRecordCreatedEvent>(OnGeneralRecordCreated);
        SubscribeLocalEvent<ESPersonalLockerComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
        SubscribeLocalEvent<ESPersonalLockerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnGeneralRecordCreated(AfterGeneralRecordCreatedEvent args)
    {
        AssignPersonalLocker(args.Key, args.Record.Name, args.Record.JobPrototype);
    }

    private void OnMapInit(Entity<ESPersonalLockerComponent> ent, ref MapInitEvent args)
    {
        _nameModifier.RefreshNameModifiers(ent.Owner);
    }

    private void OnRefreshNameModifiers(Entity<ESPersonalLockerComponent> ent, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier("comp-label-format", extraArgs: ("label", ent.Comp.Name ?? Loc.GetString("es-personal-locker-unassigned")));
    }

    public bool AssignPersonalLocker(StationRecordKey key, string name, ProtoId<JobPrototype> job)
    {
        if (!TryGetUnoccupiedPersonalLocker(job, out var locker))
            return false;

        if (TryComp<AccessReaderComponent>(locker, out var accessReader))
        {
            _accessReader.AddAccessKey((locker.Value, accessReader), key);
        }

        locker.Value.Comp.Name = name;
        locker.Value.Comp.Assigned = true;
        _nameModifier.RefreshNameModifiers(locker.Value.Owner);
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
