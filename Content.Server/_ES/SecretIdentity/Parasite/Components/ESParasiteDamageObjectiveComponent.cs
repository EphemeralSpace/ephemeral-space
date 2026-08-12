namespace Content.Server._ES.SecretIdentity.Parasite.Components;

[RegisterComponent]
[Access(typeof(ESParasiteDamageObjectiveSystem))]
public sealed partial class ESParasiteDamageObjectiveComponent : Component
{
    [DataField]
    public bool Failed;

    [DataField]
    public TimeSpan KillDelay = TimeSpan.FromMinutes(1);

    [DataField]
    public LocId Title = "es-parasite-objective-do-no-harm";
}
