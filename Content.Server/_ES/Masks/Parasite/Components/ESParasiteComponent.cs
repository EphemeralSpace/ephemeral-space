namespace Content.Server._ES.Masks.Parasite.Components;

[RegisterComponent]
[Access(typeof(ESParasiteSystem))]
public sealed partial class ESParasiteComponent : Component
{
    [DataField]
    public EntityUid? KillerMind;
}
