using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._ES.Masks.Masquerades;

/// <summary>
///     A weighted collection of masks for use by Masquerades.
/// </summary>
/// <seealso cref="MasqueradeEntry"/>
[Prototype("esMaskSet")]
public sealed partial class ESMaskSetPrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; }  = default!;

    /// <inheritdoc/>
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<ESMaskSetPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc/>
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    public IReadOnlyDictionary<ProtoId<ESMaskPrototype>, float> Masks => _masks;

    /// <summary>
    ///     A weighted random bag of masks.
    /// </summary>
    [AlwaysPushInheritance]
    [DataField("masks", required: true)]
    private Dictionary<ProtoId<ESMaskPrototype>, float> _masks = default!;

    public ProtoId<ESMaskPrototype> Pick(IRobustRandom random)
    {
        return random.Pick(_masks);
    }
}
