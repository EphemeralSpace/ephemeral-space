using Content.Server._ES.Objectives;
using Content.Server._ES.SecretIdentity.Objectives.Relays;
using Content.Server._ES.SecretIdentity.Objectives.Relays.Components;
using Content.Server._ES.SecretIdentity.WarpDisciple.Components;
using Content.Server._ES.WarpDrive;
using Content.Shared._ES.Objectives;

namespace Content.Server._ES.SecretIdentity.WarpDisciple;

public sealed partial class ESWarpUnclogObjectiveSystem : ESBaseObjectiveSystem<ESWarpUnclogObjectiveComponent>
{
    public override Type[] RelayComponents => new[] { typeof(ESWarpUnclogRelayComponent) };

    [Dependency] private ESObjectiveSystem _objectives = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESWarpUnclogObjectiveComponent, ESWarpDriveInterruptionClearedEvent>(OnInterruptionCleared);
    }

    private void OnInterruptionCleared(Entity<ESWarpUnclogObjectiveComponent> ent, ref ESWarpDriveInterruptionClearedEvent args)
    {
       _objectives.AdjustObjectiveCounter(ent.Owner);
    }
}
