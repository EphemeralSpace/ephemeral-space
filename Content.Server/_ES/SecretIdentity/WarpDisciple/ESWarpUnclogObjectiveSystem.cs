using Content.Server._ES.SecretIdentity.Objectives.Relays;
using Content.Server._ES.SecretIdentity.Objectives.Relays.Components;
using Content.Server._ES.SecretIdentity.WarpDisciple.Components;
using Content.Shared._ES.Objectives;

namespace Content.Server._ES.SecretIdentity.WarpDisciple;

public sealed partial class ESWarpUnclogObjectiveSystem : ESBaseObjectiveSystem<ESWarpUnclogObjectiveComponent>
{
    public override Type[] RelayComponents => [typeof(ESWarpUnclogRelayComponent)];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESWarpUnclogObjectiveComponent, ESWarpDriveInterruptionClearedEvent>(OnInterruptionCleared);
    }

    private void OnInterruptionCleared(Entity<ESWarpUnclogObjectiveComponent> ent, ref ESWarpDriveInterruptionClearedEvent args)
    {
       ObjectivesSys.AdjustObjectiveCounter(ent.Owner);
    }
}
