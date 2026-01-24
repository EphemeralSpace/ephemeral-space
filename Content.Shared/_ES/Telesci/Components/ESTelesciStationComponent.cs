using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Telesci.Components;

/// <summary>
/// Marks a station as supporting telescience research and its related objectives.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESSharedTelesciSystem))]
public sealed partial class ESTelesciStationComponent : Component
{
    /// <summary>
    /// Current stage of the progress
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Stage;

    /// <summary>
    /// Total number of stages
    /// </summary>
    [ViewVariables]
    public int MaxStage => Stages.Count;

    /// <summary>
    /// Configuration for each stage
    /// </summary>
    [DataField]
    public List<ESTelesciStage> Stages = [];

    /// <summary>
    /// Time it takes to evac after the last stage is complete
    /// </summary>
    [DataField]
    public TimeSpan EvacTime = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Represent data surrounding one activation of a telesci event
/// </summary>
[DataDefinition]
public partial struct ESTelesciStage
{
    /// <summary>
    /// Announcement to broadcast
    /// </summary>
    [DataField]
    public LocId Announcement;

    /// <summary>
    /// Sound effect to play
    /// </summary>
    [DataField]
    public SoundSpecifier? AnnouncementSound;

    /// <summary>
    /// Events to run
    /// </summary>
    [DataField]
    public EntityTableSelector Events = new NoneSelector();
}
