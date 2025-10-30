namespace Content.Server._ES.StationEvents.GreyTideVirus.Components;

/// <summary>
/// Used by the Grey Tide Virus event to target entities to be fucked with
/// </summary>
[RegisterComponent]
[Access(typeof(ESGreyTideVirusRule))]
public sealed partial class ESGreyTideVirusTargetComponent : Component;
