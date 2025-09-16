using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Station;

public abstract class ESSharedStationSystem : EntitySystem;

[Serializable, NetSerializable]
public sealed class ESUpdateAvailableRoundstartJobs(Dictionary<ProtoId<JobPrototype>, int?> jobs) : EntityEventArgs
{
    public Dictionary<ProtoId<JobPrototype>, int?> Jobs = jobs;
}
