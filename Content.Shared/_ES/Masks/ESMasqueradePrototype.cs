using Content.Shared._ES.Masks.Masquerades;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using YamlDotNet.Serialization.Utilities;

namespace Content.Shared._ES.Masks;

/// <summary>
/// This is a prototype for a Masquerade, a set of roles to give for given player counts.
/// </summary>
[Prototype("esMasquerade")]
public sealed partial class ESMasqueradePrototype : IPrototype, ISerializationHooks
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; }  = default!;

    /// <summary>
    ///     The name for this masquerade. Can be overwritten by localization.
    /// </summary>
    [DataField(required: true)]
    public string Name = default!;

    /// <summary>
    ///     The localized name for this masquerade.
    /// </summary>
    public string LocName(ILocalizationManager loc)
    {
        return loc.TryGetString($"es-masquerade-name-{ID}", out var value) ? value : Name;
    }

    /// <summary>
    ///     The name for this masquerade. Can be overwritten by localization.
    /// </summary>
    [DataField(required: true)]
    public string Description = default!;

    /// <summary>
    ///     The weight for this masquerade when random picking.
    ///     0 means it can never occur naturally.
    /// </summary>
    [DataField]
    public float? Weight = 1;

    /// <summary>
    ///     The localized name for this masquerade.
    /// </summary>
    public string LocDescription(ILocalizationManager loc)
    {
        return loc.TryGetString($"es-masquerade-desc-{ID}", out var value) ? value : Description;
    }
    /// <summary>
    ///     Setter for serialization because we're manually inlining some fields from MasqueradeKind.
    /// </summary>
    /// <seealso cref="MasqueradeKind.MinPlayers"/>
    [DataField(priority: 0, required: true, readOnly: true)]
    private int MinPlayers
    {
        get => 0; // So serializer doesn't get sad.
        set => Masquerade.MinPlayers = value;
    }

    /// <summary>
    ///     Setter for serialization because we're manually inlining some fields from MasqueradeKind.
    /// </summary>
    /// <seealso cref="MasqueradeKind.MaxPlayers"/>
    [DataField(priority: 0, readOnly: true)]
    private int? MaxPlayers
    {
        get => 0; // So serializer doesn't get sad.
        set => Masquerade.MaxPlayers = value;
    }


    // Due to this being shared, we can't rely on GamePresetPrototype... please don't make typos :3
    /// <summary>
    ///     The gamerules to use for this masquerade.
    /// </summary>
    [DataField(required: true, serverOnly: true)]
    public IReadOnlyList<EntProtoId> GameRules { get; private set; } = default!;

    [DataField(required: true, priority: 1)]
    public MasqueradeKind Masquerade { get; private set; } = default!;

    void ISerializationHooks.AfterDeserialization()
    {
        Masquerade.Init();
    }
}

/// <summary>
///     Base class for any masquerades. To introduce new ones, make sure you update the custom serializer too.
/// </summary>
[DataDefinition]
public abstract partial class MasqueradeKind
{
    public virtual int MinPlayers { get; set; }

    public virtual int? MaxPlayers { get; set; }

    /// <summary>
    ///     The default mask used for post-start latejoiners.
    /// </summary>
    [DataField(readOnly: true, required: true)]
    public MasqueradeEntry DefaultMask { get; set; } = default!;

    internal virtual void Init() {}
};

/// <summary>
///     A truly random masquerade. This mimics the pre-masquerades game behavior of using weights on roles.
/// </summary>
[DataDefinition]
public sealed partial class RandomMasquerade : MasqueradeKind;
