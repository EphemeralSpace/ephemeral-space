using Content.Shared.Audio;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Fluids;

/// <summary>
/// For entities that can clean up puddles
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AbsorbentComponent : Component
{
    /// <summary>
    /// Used by the client to display a bar showing the reagents contained when held.
    /// Has to still be networked in case the item is given to someone who didn't see a mop in PVS.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<Color, float> Progress = [];

    /// <summary>
    /// Name for solution container, that should be used for absorbed solution storage and as source of absorber solution.
    /// Default is 'absorbed'.
    /// </summary>
    [DataField]
    public string SolutionName = "absorbed";

    /// <summary>
    /// How much solution we can transfer in one interaction.
    /// </summary>
    [DataField]
    public FixedPoint2 PickupAmount = FixedPoint2.New(100);

    /// <summary>
    /// The effect spawned when the puddle fully evaporates.
    /// </summary>
    [DataField]
    public EntProtoId MoppedEffect = "PuddleSparkle";

    [DataField]
    public SoundSpecifier PickupSound = new SoundPathSpecifier("/Audio/Effects/Fluids/watersplash.ogg",
        AudioParams.Default.WithVariation(SharedContentAudioSystem.DefaultVariation));

    [DataField]
    public SoundSpecifier TransferSound = new SoundPathSpecifier("/Audio/Effects/Fluids/slosh.ogg",
        AudioParams.Default.WithVariation(SharedContentAudioSystem.DefaultVariation).WithVolume(-3f));

    public static readonly SoundSpecifier DefaultTransferSound =
        new SoundPathSpecifier("/Audio/Effects/Fluids/slosh.ogg",
            AudioParams.Default.WithVariation(SharedContentAudioSystem.DefaultVariation).WithVolume(-3f));

    /// <summary>
    /// Marker that absorbent component owner should try to use 'absorber solution' to replace solution to be absorbed.
    /// Target solution will be simply consumed into container if set to false.
    /// </summary>
    [DataField]
    public bool UseAbsorberSolution = true;

    /// <summary>
    /// How much reagent is actually absorbed vs being deleted. 1 = perfect efficiency, no loss of reagent.
    /// A 'less efficient' absorber is actually more ideal for janitors, since it doesn't fill up as fast.
    /// </summary>
    [DataField]
    public float Efficiency = 0.25f;
}
