using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.TileFires;

/// <summary>
///     Handles growth behavior for tile fires, as well as things like smoldering.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class ESTileFireComponent : Component
{
    /// <summary>
    ///     Prototype to spawn when spreading.
    /// </summary>
    [DataField]
    public EntProtoId Prototype = "ESTileFire";

    [DataField]
    public float MinFirestacksToSpread = 10;

    [DataField]
    public float FirestacksRemoveOnSpread = 3;

    [DataField]
    public float BaseSpreadChance = 0.33f;

    [DataField]
    public float MinimumOxyMolesToSpread = 8f;

    /// <summary>
    ///     The number of times this fire can spread.
    ///     When a fire spreads, the fire that was spawned gains the strength of this fire minus 1.
    ///     If this number is 0, the fire cannot spread.
    ///     This isn't exactly the 'radius' of a fire since it can vary depending on which fire actually spreads
    ///     + respreading and stuff but its more or less equivalent.
    ///
    ///     This is separate from firestacks etc because this is more or less "inherent" to a fire
    ///     and is a balancing vector so that different sources of fires have different strengths.
    /// </summary>
    [DataField]
    public int FireStrength = 10;

    /// <summary>
    ///     Chance that, when fire strength is passed on to a child, that strength will either not be removed or will be doubly
    ///     removed (50/50 for either)
    /// </summary>
    /// <remarks>
    ///     This is kinda just to give fires less predictable shapes and spread patterns
    /// </remarks>
    [DataField]
    public float FireStrengthRandomAlterationChance = 0.25f;

    /// <summary>
    ///     Minimum time after the fire spawns at which it will smolder (return to random stage and stop spreading)
    /// </summary>
    [DataField]
    public TimeSpan MinSmolderTime = TimeSpan.FromMinutes(3);

    /// <summary>
    ///     Maximum time after the fire spawns at which it will smolder, see <see cref="MinSmolderTime"/>
    /// </summary>
    [DataField]
    public TimeSpan MaxSmolderTime = TimeSpan.FromMinutes(3.25);

    /// <summary>
    ///     Chance that when a fire smolders its just deleted
    /// </summary>
    [DataField]
    public float SmolderDeleteFireChance = 0.33f;

    /// <summary>
    ///     Time after a fire smolders that it's deleted if <see cref="SmolderDeleteFireChance"/> procs.
    ///     This is to delay the deletion enough such that all adjacent fires are likely to have also smoldered
    ///     and will not immediately respread onto the tile that was deleted.
    /// </summary>
    [DataField]
    public TimeSpan SmolderDeleteFireTime = TimeSpan.FromMinutes(1);

    /// <summary>
    ///     Time chosen for this fire to smolder, using <see cref="MinSmolderTime"/> and <see cref="MaxSmolderTime"/>.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan SmolderTime;

    [DataField]
    public EntityUid? Origin;
}
