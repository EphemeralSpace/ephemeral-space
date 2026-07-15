using Content.Server.Atmos.EntitySystems;
using Content.Shared._ES.Filth.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Server.Containers;
using Robust.Shared.Timing;

namespace Content.Server._ES.Filth;

public sealed partial class ESMiasmaSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery;
    [Dependency] private EntityQuery<AntiRottingContainerComponent> _antiRottingContainerQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESMiasmaSourceComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ESMiasmaSourceComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateRate;
    }

    /// <summary>
    /// Checks if an entity is rotting and putting out miasma.
    /// </summary>
    public bool IsRotting(EntityUid uid)
    {
        if (_mobStateQuery.TryGetComponent(uid, out var mobState) && !_mobState.IsDead(uid, mobState))
            return false;

        if (_container.TryGetOuterContainer(uid, Transform(uid), out var container) &&
            _antiRottingContainerQuery.HasComp(container.Owner))
        {
            return false;
        }

        var ev = new IsRottingEvent();
        return !ev.Handled;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (uid, comp, xform) in EntityQueryEnumerator<ESMiasmaSourceComponent, TransformComponent>())
        {
            if (_timing.CurTime < comp.NextUpdate)
                continue;
            comp.NextUpdate += comp.UpdateRate;

            // Don't emit miasma if this mob isn't dead.
            if (!IsRotting(uid))
                continue;

            var mixture = _atmosphere.GetTileMixture((uid, xform));
            mixture?.AdjustMoles(Gas.Miasma, (float) (comp.MolPerSecond * comp.UpdateRate.TotalSeconds));
        }
    }
}
