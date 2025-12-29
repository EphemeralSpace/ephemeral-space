using Content.Server._ES.StationVariation.Components;
using Content.Server._ES.StationVariation.Components.ReplacementMarkers;
using Content.Server.GameTicking.Rules.VariationPass;

namespace Content.Server._ES.StationVariation.Systems;

/// <summary>
///     Handles replacing light entities marked with <see cref="ESNightshiftReplacementMarkerComponent"/>, for nightshift lighting.
/// </summary>
public sealed class ESNightshiftVariationPassSystem : BaseEntityReplaceVariationPassSystem<
    ESNightshiftReplacementMarkerComponent, ESNightshiftVariationPassComponent>;
