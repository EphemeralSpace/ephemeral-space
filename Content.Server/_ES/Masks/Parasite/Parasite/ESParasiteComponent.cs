using Content.Shared.Mind;

namespace Content.Server._ES.Masks.Parasite;

[RegisterComponent]
public sealed partial class ESParasiteComponent : Component
{
    public Entity<MindComponent>? KillerMind;
}
