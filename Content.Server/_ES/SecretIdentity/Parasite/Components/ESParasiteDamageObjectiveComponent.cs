namespace Content.Server._ES.SecretIdentity.Parasite.Components;

[RegisterComponent]
[Access(typeof(ESParasiteDamageObjectiveSystem))]
public sealed partial class ESParasiteDamageObjectiveComponent : Component
{
    [DataField]
    public bool Failed;

    [DataField]
    public TimeSpan KillDelay = TimeSpan.FromMinutes(1);

    /// <summary>
    ///     Progress when the last amount of damage was dealt.
    ///     Used to determine when to show the warning popups.
    /// </summary>
    [DataField]
    public float LastProgress;

    [DataField]
    public LocId Title = "es-parasite-objective-do-no-harm";
}
