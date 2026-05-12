using System.Linq;
using Content.Server._ES.Masks.Masquerades;
using Content.Server.Mind;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.Masks;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.Masks.Superfan;

/// <seealso cref="ESSuperfanComponent"/>
public sealed partial class ESSuperfanSystem : EntitySystem
{
    [Dependency] private ESMaskSystem _mask = default!;
    [Dependency] private ESMasqueradeSystem _masquerade = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private static readonly ProtoId<ESTroupePrototype> TraitorsTroupe = "Traitor";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESPlayerKilledEvent>(OnKillReported);
    }

    private void OnKillReported(ref ESPlayerKilledEvent ev)
    {
        // Only activate if our target troupe died.
        if (_mask.GetTroupeOrNull(ev.Killed) != TraitorsTroupe)
            return;

        if (!_masquerade.TryGetMasqueradeData(out var set))
            return; // Well, no masquerade means no conversion target.

        if (set.SuperfanTarget is not { } entry)
        {
            // Fail silently, we were never configured to begin with. See #1079
            return;
        }

        var total = 0;
        var dead = 0;
        foreach (var member in _mask.GetTroupeMembers(TraitorsTroupe))
        {
            total += 1;

            if (_mind.IsCharacterDeadIc(Comp<MindComponent>(member)))
                dead += 1;
        }

        // Chance to be converted is proportional to the number of dead troupe members.
        var prob = total != 0
            ? (float)dead / total
            : 1;

        var fanQuery = EntityQueryEnumerator<ESSuperfanComponent, MindComponent>();
        while (fanQuery.MoveNext(out var ent, out _, out var mind))
        {
            if (!_random.Prob(prob))
                continue;

            if (_mind.IsCharacterDeadIc(mind))
                continue; // Don't assign the dead to tot masks.

            _mask.ChangeMask((ent, mind), entry.PickMasks(_random, _proto).Single());
        }
    }
}
