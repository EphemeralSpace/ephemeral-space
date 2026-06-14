namespace Content.Server._ES.Masks.Parasite.Components;

[RegisterComponent]
[Access(typeof(ESParasiteDamageObjectiveSystem))]
public sealed partial class ESParasiteDamageObjectiveComponent : Component
{
    [DataField]
    public bool Failed;

    [DataField]
    public TimeSpan KillDelay = TimeSpan.FromMinutes(1);
}
