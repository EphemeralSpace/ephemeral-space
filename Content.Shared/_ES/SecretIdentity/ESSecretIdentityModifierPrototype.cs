using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.SecretIdentity;

/// <summary>
/// Modifiers applied from <see cref="ESSecretIdentityPrototype"/> that add additional behavior.
/// </summary>
[Prototype("esSecretIdentityModifier")]
public sealed partial class ESSecretIdentityModifierPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The player-facing name of the modifier
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// The color of the modifier in the UI
    /// </summary>
    [DataField]
    public Color Color = Color.White;

    [DataField(required: true)]
    public ESSecretIdentifierModifierEvent Event = default!;
}

[Serializable, NetSerializable]
[ImplicitDataDefinitionForInheritors]
public abstract partial class ESSecretIdentifierModifierEvent : EntityEventArgs;
