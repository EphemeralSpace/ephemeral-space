using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Server._ES.Masks.Vigilante.Components;
using Content.Shared._ES.KillTracking;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.Objectives;

namespace Content.Server._ES.Masks.Vigilante;

public sealed class ESKillKillerObjectiveSystem : ESBaseObjectiveSystem<ESKillKillerObjectiveComponent>
{
    [Dependency] private readonly ESKillTrackingSystem _killTracking = default!;

    public override Type[] RelayComponents => [typeof(ESKilledRelayComponent)];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESKillKillerObjectiveComponent, ESKilledPlayerEvent>(OnKill);
    }

    private void OnKill(Entity<ESKillKillerObjectiveComponent> ent, ref ESKilledPlayerEvent args)
    {
        // Suicides don't count and i'll enumerate why because it's funny:
        // If you can suicide to get the kill, then the best strategy is to murder someone at complete random
        // If you randomly get a killer. Great! If not, you can just suicide at any point during the round,
        // and it will count as a valid completion.
        //
        // It's fun to think about, though.
        if (args.Suicide)
            return;

        if (_killTracking.GetPlayerKillCount(args.Killed) > 0)
            ObjectivesSys.AdjustObjectiveCounter(ent.Owner);
    }
}
