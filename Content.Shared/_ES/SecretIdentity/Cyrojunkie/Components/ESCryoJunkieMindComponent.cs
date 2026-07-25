using Content.Shared._ES.Core.Timer.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.SecretIdentity.Cyrojunkie.Components;

[RegisterComponent]
public sealed partial class ESCryoJunkieMindComponent : Component
{
    [DataField]
    public TimeSpan HuskDelay = TimeSpan.FromSeconds(30);
}

[Serializable, NetSerializable]
public sealed partial class ESCyroJunkieTimerEvent : ESEntityTimerEvent;
