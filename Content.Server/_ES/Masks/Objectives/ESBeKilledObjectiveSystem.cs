using Content.Server._ES.Masks.Objectives.Components;
using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.Objectives;

namespace Content.Server._ES.Masks.Objectives;

/// <summary>
///     Handles objective logic for player-kills (e.g. for jester masks)
/// </summary>
public sealed class ESBeKilledObjectiveSystem : ESBaseObjectiveSystem<ESBeKilledObjectiveComponent>
{
    public override Type[] RelayComponents => [typeof(ESKilledRelayComponent)];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESBeKilledObjectiveComponent, ESPlayerKilledEvent>(OnKilled);
    }

    private void OnKilled(Entity<ESBeKilledObjectiveComponent> ent, ref ESPlayerKilledEvent args)
    {
        if (!args.ValidKill ||
            !MindSys.TryGetMind(args.Killer.Value, out var mind) ||
            MaskSys.GetTroupeOrNull(mind.Value.AsNullable()) != ent.Comp.TroupeRequired)
            return;

        ObjectivesSys.SetObjectiveCounter(ent.Owner, 1f);
    }
}
