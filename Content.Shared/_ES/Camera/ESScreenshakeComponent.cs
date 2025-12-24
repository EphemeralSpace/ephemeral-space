using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Camera;

/// <summary>
///     Marks an entity which is actively screenshaking because of a screenshake command being given.
/// </summary>
/// <remarks>
///     This doesn't mark an entity which *can* screenshake--all entities can, by default, as long as a client is controlling them.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESScreenshakeComponent : Component
{
    /// <summary>
    ///     A set of screenshake commands which this entity is currently processing.
    ///     A "trauma" of 0 means no change, and a trauma of 100 means max angle/offset.
    /// </summary>
    /// <remarks>
    ///     This is a set, because order doesn't matter, and we don't want to accidentally readd the same command twice.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public HashSet<ESScreenshakeCommand> Commands = new();

    [DataField, AutoNetworkedField]
    public float TranslationalDecayRate = 2f;

    [DataField, AutoNetworkedField]
    public float RotationalDecayRate = 2f;

    public override bool SendOnlyToOwner => true;
}

/// <summary>
///     Represents a single screenshake command. These are stored and networked on <see cref="ESScreenshakeComponent"/>,
///     and the client that controls that entity will use the trauma values in each command, and their start time,
///     to calculate multipliers on the current eye offset & rotation modifiers.
/// </summary>
/// <param name="TranslationalTrauma">Strength of translational screenshake (offset-based)</param>
/// <param name="RotationalTrauma">Strength of rotational screenshake (rotation-based)</param>
/// <param name="Start">Time this screenshake command was added.</param>
/// <param name="CalculatedEnd">The end time for this command, calculated from trauma, decay rate, and start time.</param>
/// <param name="Frequency">Since screenshake is sampled from simplex noise, this controls the frequency passed to the underlying noise.</param>
[DataRecord, Serializable, NetSerializable]
public partial record struct ESScreenshakeCommand(float TranslationalTrauma, float RotationalTrauma, TimeSpan Start, TimeSpan CalculatedEnd, float Frequency = 0.01f);
