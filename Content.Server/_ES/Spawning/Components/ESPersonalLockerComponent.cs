using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Spawning.Components;

[RegisterComponent]
[Access(typeof(ESPersonalLockerSystem))]
public sealed partial class ESPersonalLockerComponent : Component
{
    [DataField]
    public bool Assigned;

    [DataField]
    public string? Name;

    [DataField(required: true)]
    public ProtoId<JobPrototype> Job;
}
