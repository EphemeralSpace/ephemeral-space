using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Filth.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ESDiseaseCloudComponent : Component
{
    [DataField]
    public DamageSpecifier DiseaseDamage = new();

    [DataField]
    public SoundSpecifier? DiseaseSound = new SoundCollectionSpecifier("BoxingHit");
}
