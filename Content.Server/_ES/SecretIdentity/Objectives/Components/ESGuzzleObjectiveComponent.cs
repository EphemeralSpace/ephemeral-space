using Content.Shared._ES.Objectives.Components;

namespace Content.Server._ES.SecretIdentity.Objectives.Components;

/// <summary>
///     THE GUZZLER WUZ HERE!!!
///     This contains data for a particular kind of objective that requires imbibing X amount of reagents, total.
///     If you're here from the guzzler design doc, this does not handle the specific request reagent, that's in
///     <see cref="ESImbibeReagentObjectiveComponent"/>.
///     The target is set by <see cref="ESCounterObjectiveComponent"/>.
/// </summary>
/// <seealso cref="ESGuzzleObjectiveSystem"/>
[RegisterComponent]
[Access(typeof(ESGuzzleObjectiveSystem))]
public sealed partial class ESGuzzleObjectiveComponent : Component;
