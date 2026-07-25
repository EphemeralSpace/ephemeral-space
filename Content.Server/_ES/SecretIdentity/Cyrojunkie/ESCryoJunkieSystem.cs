using Content.Server._ES.Cryohusk;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Ghost;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.SecretIdentity.Cyrojunkie.Components;
using Content.Shared.Administration.Systems;
using Content.Shared.Atmos;
using Content.Shared.Mind.Components;

namespace Content.Server._ES.SecretIdentity.Cyrojunkie;

public sealed partial class ESCryoJunkieSystem : EntitySystem
{
    [Dependency] private ESCryohuskSystem _cryo = default!;
    [Dependency] private ESEntityTimerSystem _entityTimer = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private RejuvenateSystem _rejuvenate = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ESCryoJunkieMindComponent, GhostAttemptHandleEvent>(OnGhostAttempt);
        SubscribeLocalEvent<MindContainerComponent, ESCyroJunkieTimerEvent>(OnCyroJunkieTimer);
    }

    private void OnCyroJunkieTimer(Entity<MindContainerComponent> ent, ref ESCyroJunkieTimerEvent args)
    {
        var mixture = _atmosphere.GetTileMixture(ent.Owner);
        mixture?.AdjustMoles(Gas.Cryogas, 200);

        _rejuvenate.PerformRejuvenate(ent.Owner);
        _cryo.Cryohusk(ent.Owner);
    }

    private void OnGhostAttempt(Entity<ESCryoJunkieMindComponent> ent, ref GhostAttemptHandleEvent args)
    {
        if (args.Mind.Comp.CurrentEntity == null)
            return;

        _entityTimer.SpawnTimer((EntityUid)args.Mind.Comp.CurrentEntity, ent.Comp.HuskDelay, new ESCyroJunkieTimerEvent());

        args.Result = true;
        args.Handled = true;

        RemComp(ent, ent.Comp);
    }
}
