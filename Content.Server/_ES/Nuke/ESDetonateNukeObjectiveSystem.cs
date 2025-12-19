using Content.Server._ES.Nuke.Components;
using Content.Server.Nuke;
using Content.Shared._ES.Objectives;

namespace Content.Server._ES.Nuke;

public sealed class ESDetonateNukeObjectiveSystem : ESBaseObjectiveSystem<ESDetonateNukeObjectiveComponent>
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NukeExplodedEvent>(OnNukeExploded);
    }

    private void OnNukeExploded(NukeExplodedEvent ev)
    {
        ObjectivesSys.AdjustObjectiveCounter<ESDetonateNukeObjectiveComponent>();
    }
}
