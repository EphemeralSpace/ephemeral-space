using Content.Shared.Polymorph;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._ES.Cryohusk.Components;

/// <summary>
/// Marks an entity as able to be converted into a cryohusk
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(ESCryohuskSystem))]
public sealed partial class ESCryohuskableComponent : Component
{
    [DataField]
    public float MinConversionMols = 2.5f;

    [DataField]
    public float ConversionChance = 0.33f;

    [DataField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(10f);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;

    [DataField]
    public ProtoId<PolymorphPrototype> CryohuskPolymorph = "ESCryohuskPolymorph";

    [DataField]
    public SoundSpecifier? FreezeSound = new SoundCollectionSpecifier("ESFreeze")
    {
        Params = new AudioParams().WithVolume(5f),
    };
}
