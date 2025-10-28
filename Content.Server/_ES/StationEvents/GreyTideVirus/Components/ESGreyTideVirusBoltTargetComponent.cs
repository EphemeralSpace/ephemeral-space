namespace Content.Server._ES.StationEvents.GreyTideVirus.Components;

/// <summary>
/// Used by the Grey Tide Virus event to target doors to be bolted
/// </summary>
[RegisterComponent]
[Access(typeof(ESGreyTideVirusRule))]
public sealed partial class ESGreyTideVirusBoltTargetComponent : Component;
