using Content.Shared._ES.Objectives.Target.Components;
using Content.Shared.Mind;

namespace Content.Shared._ES.Objectives.Target;

public sealed class ESTargetSelfObjectiveSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESTargetSelfObjectiveComponent, ESGetObjectiveTargetCandidates>(GetCandidates);
    }

    private void GetCandidates(Entity<ESTargetSelfObjectiveComponent> ent, ref ESGetObjectiveTargetCandidates args)
    {
        if (TryComp<MindComponent>(args.Holder, out var mind) && mind.OwnedEntity is { } ownedEntity)
            args.Candidates.Add(ownedEntity);
    }
}
