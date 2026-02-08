using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Telesci.Anomaly.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESSharedAnomalySystem))]
public sealed partial class ESPortalAnomalyComponent : Component
{
    [DataField, AutoNetworkedField]
    public int CodeIndex;

    [DataField, AutoNetworkedField]
    public List<ESAnomalySignal> SignalCode = [];

    [DataField]
    public int CodeLength = 4;

    [DataField]
    public SoundSpecifier? SignalSound = new SoundPathSpecifier("/Audio/Machines/quickbeep.ogg");

    [DataField]
    public SoundSpecifier? RadPulseSound = new SoundCollectionSpecifier("RadiationPulse");

    [DataField]
    public EntProtoId RadiationEntity = "ESAnomalyRadPulse";
}

[Serializable, NetSerializable]
public sealed class ESAnomalyCollapseAnimationEvent : EntityEventArgs
{
    public NetEntity Anomaly;
}

[Serializable, NetSerializable]
public sealed class ESAnomalyShrinkAnimationEvent : EntityEventArgs
{
    public NetEntity Anomaly;
}

[Serializable, NetSerializable]
public sealed class ESAnomalyRadiationAnimationEvent : EntityEventArgs
{
    public NetEntity Anomaly;
}

[Serializable, NetSerializable]
public enum ESAnomalySignal : byte
{
    Alpha,
    Beta,
    Gamma,
    Delta,
    Epsilon,
    Zeta,
}
