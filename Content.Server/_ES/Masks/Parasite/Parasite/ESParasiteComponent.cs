using Content.Shared.Mind;

namespace Content.Server._ES.Masks.Parasite;

[RegisterComponent]
public sealed partial class ESParasiteComponent : Component
{
    [DataField]
    public EntityUid? KillerMind;
}
