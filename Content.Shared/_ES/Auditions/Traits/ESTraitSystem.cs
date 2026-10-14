using System.Linq;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._ES.Auditions.Traits;

public sealed partial class ESTraitSystem : EntitySystem
{
    public override void Initialize()
    {
        InitializeEvents();
    }

    public void ApplyTrait(EntityUid target, IEnumerable<ProtoId<ESTraitPrototype>> traits)
    {
        foreach (var trait in traits)
        {
            ApplyTrait(target, trait);
        }
    }

    public void ApplyTrait(EntityUid target, ProtoId<ESTraitPrototype> traitId)
    {
        ApplyTrait(target, ProtoMan.Index(traitId));
    }

    public void ApplyTrait(EntityUid target, ESTraitPrototype trait)
    {
        foreach (var ev in trait.Events)
        {
            RaiseLocalEvent(target, (object) ev);
        }
    }

    public HashSet<ProtoId<ESTraitPrototype>> GetRandomTraits(IRobustRandom random)
    {
        var outTraits = new HashSet<ProtoId<ESTraitPrototype>>();
        var traitGroups = ProtoMan.EnumeratePrototypes<ESTraitPrototype>()
            .GroupBy(p => p.Category);

        foreach (var grouping in traitGroups)
        {
            // uncategorized traits have unique logic
            if (!grouping.Key.HasValue)
            {
                foreach (var trait in grouping)
                {
                    if (random.Prob(trait.Prob))
                        outTraits.Add(trait);
                }
                continue;
            }

            var category = ProtoMan.Index(grouping.Key.Value);
            if (!random.Prob(category.Prob))
                continue;

            var weights = grouping.Select(p => (p, p.Prob)).ToDictionary();
            outTraits.Add(random.Pick(weights));
        }

        return outTraits;
    }
}
