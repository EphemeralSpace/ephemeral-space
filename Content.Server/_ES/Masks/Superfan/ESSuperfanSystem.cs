using System.Linq;
using Content.Server._ES.Masks.Masquerades;
using Content.Server.KillTracking;
using Content.Server.Mind;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.Masks;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._ES.Masks.Superfan;

/// <seealso cref="ESSuperfanComponent"/>
public sealed class ESSuperfanSystem : EntitySystem
{
    [Dependency] private readonly ESMaskSystem _mask = default!;
    [Dependency] private readonly ESMasqueradeSystem _masquerade = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private static readonly ProtoId<ESTroupePrototype> TraitorsTroupe = "ESTraitor";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESPlayerKilledEvent>(OnKillReported);
    }

    private void OnKillReported(ref ESPlayerKilledEvent ev)
    {
        // TODO: This feels fishy. I'll leave the kill reporting rewrite nerds to
        //       figure out having a kill report for entire troupes down the line.
        foreach (var member in _mask.GetTroupeMembers(TraitorsTroupe))
        {
            if (!_mind.IsCharacterDeadIc(Comp<MindComponent>(member)))
                return; // Well the troupe ain't dead.
        }

        if (!_masquerade.TryGetMasqueradeData(out var set))
            return; // Well, no masquerade means no conversion target.

        if (set.SuperfanTarget is not { } entry)
        {
            // Fail silently, we were never configured to begin with. See #1079
            return;
        }

        var fanQuery = EntityQueryEnumerator<ESSuperfanComponent, MindComponent>();

        while (fanQuery.MoveNext(out var ent, out var fan, out var mind))
        {
            if (_mind.IsCharacterDeadIc(mind))
                continue; // Don't assign the dead to tot masks.

            _mask.ChangeMask((ent, mind), entry.PickMasks(_random, _proto).Single());
        }
    }
}
