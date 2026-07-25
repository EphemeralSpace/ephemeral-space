using Content.Shared.Polymorph;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._ES.Cryohusk.Components;

/// <summary>
/// Marks an entity as able to be converted into a cryohusk
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
[Access(typeof(ESSharedCryohuskSystem))]
public sealed partial class ESCryohuskableComponent : Component
{
    [DataField]
    public float MinConversionMols = 2.0f;

    [DataField]
    public float ConversionChance = 0.5f;

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
