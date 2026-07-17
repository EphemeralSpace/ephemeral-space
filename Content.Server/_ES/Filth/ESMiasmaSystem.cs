using Content.Server.Atmos.EntitySystems;
using Content.Server.Decals;
using Content.Shared._ES.Filth.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Decals;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Server.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._ES.Filth;

public sealed partial class ESMiasmaSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private DecalSystem _decal = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery;
    [Dependency] private EntityQuery<AntiRottingContainerComponent> _antiRottingContainerQuery;

    public const int MaxDirtDecalsPerTile = 3;

    private readonly ProtoId<DecalPrototype>[] _dirtDecals =
    [
        "Dirt",
        "DirtLight",
        "DirtMedium",
        "DirtHeavy",
    ];

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
    public bool IsRotting(Entity<ESMiasmaSourceComponent> ent)
    {
        if (ent.Comp.RequireDead && _mobStateQuery.TryGetComponent(ent, out var mobState) && !_mobState.IsDead(ent, mobState))
            return false;

        if (_container.TryGetOuterContainer(ent, Transform(ent), out var container) &&
            _antiRottingContainerQuery.HasComp(container.Owner))
        {
            return false;
        }

        var ev = new IsRottingEvent();
        return !ev.Handled;
    }

    /// <summary>
    ///     Tries to add burnt decals to a tile, counting them and stopping at a maximum of 4.
    /// </summary>
    public void TryAddDirtDecalsToTile(EntityUid gridUid, Vector2i tilePos, int count = 1, int maxPerTile = MaxDirtDecalsPerTile)
    {
        // Get the existing decals on the tile
        var tileDecals = _decal.GetDecalsInRange(gridUid, tilePos);

        // Count the burnt decals on the tile
        var decalCount = 0;

        foreach (var set in tileDecals)
        {
            if (Array.IndexOf(_dirtDecals, set.Decal.Id) == -1)
                continue;

            decalCount++;

            if (decalCount >= maxPerTile)
                return;
        }

        for (var i = 0; i < count; i++)
        {
            // Add a random burned decal to the tile only if there are less than 4 of them
            if (decalCount >= maxPerTile)
                return;

            _decal.TryAddDecal(_dirtDecals[_random.Next(_dirtDecals.Length)],
                new EntityCoordinates(gridUid, tilePos),
                out _,
                cleanable: true);

            decalCount += 1;
        }
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
            if (!IsRotting((uid, comp)))
                continue;

            var mixture = _atmosphere.GetTileMixture((uid, xform), excite: true);
            mixture?.AdjustMoles(Gas.Miasma, (float) (comp.MolPerSecond * comp.UpdateRate.TotalSeconds));
        }
    }
}
