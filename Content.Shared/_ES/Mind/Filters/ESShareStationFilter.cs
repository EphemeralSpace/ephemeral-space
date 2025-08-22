using Content.Shared._ES.Auditions.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Filters;
using JetBrains.Annotations;

namespace Content.Shared._ES.Mind.Filters;

/// <summary>
/// Mind filter that excludes people who do not share the same station.
/// </summary>
[UsedImplicitly]
public sealed partial class ESShareStationFilter : MindFilter
{
    protected override bool ShouldRemove(Entity<MindComponent> mind, EntityUid? exclude, IEntityManager entMan, SharedMindSystem mindSys)
    {
        // IMPL. NOTE: "exclude" is the mind of the person the objective is being assigned to.
        // For some reason the objective is never passed any other way, but this seems to be how its done.
        // Why? Fuck you! that's why. Dogass API.
        if (!entMan.TryGetComponent<ESCharacterComponent>(exclude, out var character))
            return false;

        // IMPL. NOTE: I implemented this while drinking a pomegranate and blueberry bubble tea with tapioca.
        if (!entMan.TryGetComponent<ESCharacterComponent>(mind, out var otherCharacter))
            return false;

        return character.Station != otherCharacter.Station;
    }
}
