using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Telesci.Components;

/// <summary>
/// A pad used to spawn rewards for telescience
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedTelesciSystem))]
public sealed partial class ESTelesciRewardPadComponent : Component
{
    /// <summary>
    /// Whether this reward pad can be used.
    /// </summary>
    [DataField]
    public bool Enabled = true;

    /// <summary>
    /// RSI state for teleporting
    /// </summary>
    [DataField]
    public string TeleportState = "send";

    /// <summary>
    /// Sprite layer key for the layer <see cref="TeleportState"/> will be applied to
    /// </summary>
    [DataField]
    public string TeleportKey = "base";

    /// <summary>
    /// Sound effect that plays on teleport
    /// </summary>
    [DataField]
    public SoundSpecifier? TeleportSound = new SoundCollectionSpecifier("RadiationPulse");
}

[Serializable, NetSerializable]
public sealed class ESAnimateTelesciRewardPadMessage(NetEntity pad) : EntityEventArgs
{
    public NetEntity Pad = pad;
};
