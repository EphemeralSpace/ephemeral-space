using Content.Server._ES.Nuke.Components;
using Content.Server.Nuke;
using Content.Shared.Nuke;
using Content.Shared.Objectives.Components;

namespace Content.Server._ES.Nuke;

public sealed class ESDetonateNukeObjectiveSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESDetonateNukeObjectiveComponent, ObjectiveGetProgressEvent>(OnGetProgress);
        SubscribeLocalEvent<NukeExplodedEvent>(OnNukeExploded);
    }

    private void OnGetProgress(Entity<ESDetonateNukeObjectiveComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        if (ent.Comp.Detonated)
        {
            args.Progress = 1f;
            return;
        }

        var highestStage = NukeStatus.AWAIT_DISK;
        var query = EntityQueryEnumerator<NukeComponent>();
        while (query.MoveNext(out var comp))
        {
            if (comp.Status > highestStage)
                highestStage = comp.Status;
        }

        args.Progress = highestStage switch
        {
            NukeStatus.AWAIT_CODE => 0.25f,
            NukeStatus.AWAIT_ARM => 0.5f,
            NukeStatus.ARMED => 0.75f,
            _ => 0,
        };
    }

    private void OnNukeExploded(NukeExplodedEvent ev)
    {
        var query = EntityQueryEnumerator<ESDetonateNukeObjectiveComponent>();
        while (query.MoveNext(out var comp))
        {
            // Just assume it's in the right place.
            comp.Detonated = true;
        }
    }
}
