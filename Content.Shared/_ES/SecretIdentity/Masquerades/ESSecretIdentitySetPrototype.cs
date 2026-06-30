using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared._ES.SecretIdentity.Masquerades;

/// <summary>
///     A weighted collection of masks for use by Masquerades.
/// </summary>
/// <seealso cref="MasqueradeEntry"/>
[Prototype("esSecretIdentitySet")]
public sealed partial class ESSecretIdentitySetPrototype : IPrototype, IInheritingPrototype, ISerializationHooks
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; }  = default!;

    /// <inheritdoc/>
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<ESSecretIdentitySetPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc/>
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    [AlwaysPushInheritance]
    [DataField("maskProvider")]
    private MaskSetProvider? _secretIdentitySetProvider = default!;

    /// <summary>
    ///     A weighted random bag of masks.
    /// </summary>
    [AlwaysPushInheritance]
    [DataField("masks")]
    private Dictionary<ProtoId<ESSecretIdentityPrototype>, float>? _secretIdentitys = default!;

    public List<ProtoId<ESSecretIdentityPrototype>> Pick(IRobustRandom random, int count)
    {
        if (_secretIdentitys is not null)
            return Enumerable.Range(0, count).Select(_ => random.Pick(_secretIdentitys)).ToList();
        else
            return _secretIdentitySetProvider!.Pick(random, count);
    }

    public IEnumerable<ProtoId<ESSecretIdentityPrototype>> AllMasks()
    {
        if (_secretIdentitys is not null)
            return _secretIdentitys.Keys;
        else
            return _secretIdentitySetProvider!.AllMasks();
    }

    void ISerializationHooks.AfterDeserialization()
    {
        DebugTools.Assert(_secretIdentitys is null ^ _secretIdentitySetProvider is null, $"You need to specify ONE of masks or maskProvider on mask set {ID}");
    }
}

public abstract class MaskSetProvider
{
    private bool _injected = false; // Due to the weird spot this is in, we kinda just gotta eat an IOC injection.

    public List<ProtoId<ESSecretIdentityPrototype>> Pick(IRobustRandom random, int count)
    {
        EnsureInjected();

        return PickInner(random, count);
    }

    public IEnumerable<ProtoId<ESSecretIdentityPrototype>> AllMasks()
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

    protected abstract List<ProtoId<ESSecretIdentityPrototype>> PickInner(IRobustRandom random, int count);

    protected abstract IEnumerable<ProtoId<ESSecretIdentityPrototype>> AllMasksInner();
}

[DataDefinition]
public sealed partial class ESTroupeMasksProvider : MaskSetProvider
{
    [Dependency]
    private IPrototypeManager _proto = default!;

    [DataField(required: true)]
    public ProtoId<ESTroupePrototype> Troupe = "Crew";

    private Dictionary<ProtoId<ESSecretIdentityPrototype>, float>? _secretIdentitys = null;

    [MemberNotNull(nameof(_secretIdentitys))]
    private void Init()
    {
        if (_secretIdentitys is not null)
            return;

        _secretIdentitys = _proto.EnumeratePrototypes<ESSecretIdentityPrototype>()
            .Where(x => x.Troupe == Troupe)
            .ToDictionary(x => new ProtoId<ESSecretIdentityPrototype>(x.ID), x => x.Weight);
    }

    protected override List<ProtoId<ESSecretIdentityPrototype>> PickInner(IRobustRandom random, int count)
    {
        Init();

        return Enumerable.Range(0, count).Select(_ => random.Pick(_secretIdentitys)).ToList();
    }

    protected override IEnumerable<ProtoId<ESSecretIdentityPrototype>> AllMasksInner()
    {
        Init();

        return _secretIdentitys.Keys;
    }
}
