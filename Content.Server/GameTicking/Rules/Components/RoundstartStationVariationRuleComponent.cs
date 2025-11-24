using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// This handles starting various roundstart variation rules after a station has been loaded.
/// </summary>
[RegisterComponent]
public sealed partial class RoundstartStationVariationRuleComponent : Component
{
    /// <summary>
    ///     The list of rules that will be started once the map is spawned.
    ///     Uses <see cref="EntitySpawnEntry"/> to support probabilities for various rules
    ///     without having to hardcode the probability directly in the rule's logic.
    /// </summary>
    // ES START required to false
    [DataField(required: false)]
    // ES END
    public List<EntitySpawnEntry> Rules = new();

    // ES START
    // oh god i just want it to be an entity table im sorry when ifirst made this system those didnt exist
    [DataField]
    public EntityTableSelector RulesTable = new NoneSelector();
    // ES END
}
