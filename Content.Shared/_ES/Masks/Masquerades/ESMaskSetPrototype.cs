using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Masks.Masquerades;

/// <summary>
///     A weighted collection of masks for use by Masquerades.
/// </summary>
/// <seealso cref="MasqueradeEntry"/>
[Prototype("esMaskSet")]
public sealed partial class ESMaskSetPrototype : IPrototype, IInheritingPrototype, ISerializationHooks
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

    [AlwaysPushInheritance]
    [DataField("maskProvider")]
    private MaskSetProvider? _maskSetProvider = default!;

    /// <summary>
    ///     A weighted random bag of masks.
    /// </summary>
    [AlwaysPushInheritance]
    [DataField("masks")]
    private Dictionary<ProtoId<ESMaskPrototype>, float>? _masks = default!;

    public List<ProtoId<ESMaskPrototype>> Pick(IRobustRandom random, int count)
    {
        if (_masks is not null)
            return Enumerable.Range(0, count).Select(_ => random.Pick(_masks)).ToList();
        else
            return _maskSetProvider!.Pick(random, count);
    }

    public IEnumerable<ProtoId<ESMaskPrototype>> AllMasks()
    {
        if (_masks is not null)
            return _masks.Keys;
        else
            return _maskSetProvider!.AllMasks();
    }

    void ISerializationHooks.AfterDeserialization()
    {
        DebugTools.Assert(_masks is null ^ _maskSetProvider is null, $"You need to specify ONE of masks or maskProvider on mask set {ID}");
    }
}

public abstract class MaskSetProvider
{
    private bool _injected = false; // Due to the weird spot this is in, we kinda just gotta eat an IOC injection.

    public List<ProtoId<ESMaskPrototype>> Pick(IRobustRandom random, int count)
    {
        EnsureInjected();

        return PickInner(random, count);
    }

    public IEnumerable<ProtoId<ESMaskPrototype>> AllMasks()
    {
        EnsureInjected();

        return AllMasksInner();
    }

    private void EnsureInjected()
    {
        if (!_injected)
            IoCManager.InjectDependencies(this);

        _injected = true;
    }

    protected abstract List<ProtoId<ESMaskPrototype>> PickInner(IRobustRandom random, int count);

    protected abstract IEnumerable<ProtoId<ESMaskPrototype>> AllMasksInner();
}

[DataDefinition]
public sealed partial class ESTroupeMasksProvider : MaskSetProvider
{
    [Dependency]
    private readonly IPrototypeManager _proto = default!;

    [DataField(required: true)]
    public ProtoId<ESTroupePrototype> Troupe = "ESCrew";

    private Dictionary<ProtoId<ESMaskPrototype>, float>? _masks = null;

    [MemberNotNull(nameof(_masks))]
    private void Init()
    {
        if (_masks is not null)
            return;

        _masks = _proto.EnumeratePrototypes<ESMaskPrototype>()
            .Where(x => x.Troupe == Troupe)
            .ToDictionary(x => new ProtoId<ESMaskPrototype>(x.ID), x => x.Weight);
    }

    protected override List<ProtoId<ESMaskPrototype>> PickInner(IRobustRandom random, int count)
    {
        Init();

        return Enumerable.Range(0, count).Select(_ => random.Pick(_masks)).ToList();
    }

    protected override IEnumerable<ProtoId<ESMaskPrototype>> AllMasksInner()
    {
        Init();

        return _masks.Keys;
    }
}
