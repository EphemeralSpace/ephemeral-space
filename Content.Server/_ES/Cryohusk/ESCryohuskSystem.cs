using Content.Server._ES.Objectives;
using Content.Server._ES.SecretIdentity.Cyrojunkie.Components;
using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Mind;
using Content.Server.Polymorph.Systems;
using Content.Shared._ES.Cryohusk;
using Content.Shared._ES.Cryohusk.Components;
using Content.Shared._ES.Stagehand;
using Content.Shared.Access.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Content.Shared.Mobs.Systems;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Toolshed;

namespace Content.Server._ES.Cryohusk;

public sealed partial class ESCryohuskSystem : ESSharedCryohuskSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private ESObjectiveSystem _objective = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private ESSharedStagehandNotificationsSystem _stagehandNotifications = default!;

    [Dependency] private EntityQuery<IdCardComponent> _idCardQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESCryohuskableComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ESCryohuskableComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime;
    }

    public override void Cryohusk(Entity<ESCryohuskableComponent?> target, bool transferDeath = true)
    {
        if (!Resolve(target, ref target.Comp))
            return;

        if (_polymorph.PolymorphEntity(target, target.Comp.CryohuskPolymorph, transferDamageOverride: transferDeath) is not { } husk)
            return;

        foreach (var uid in GetRecursiveContainedEntities(husk))
        {
            if (_idCardQuery.HasComp(uid))
                EnsureComp<ESCryohuskIdCardComponent>(uid);
        }

        if (_mind.TryGetMind(husk, out var mind))
        {
            if (!_mobState.IsDead(target) || !transferDeath)
            {
                var msg = Loc.GetString("es-cryohusk-convert-stagehand-notif",
                    ("player", _stagehandNotifications.WrapEntityName(target.Owner)));
                _stagehandNotifications.SendStagehandNotification(msg);
            }

            foreach (var objective in _objective.GetObjectives<ESCryohuskObjectiveComponent>(mind.Value.Owner))
            {
                _objective.AdjustObjectiveCounter(objective.Owner);
            }
        }

        _audio.PlayPvs(target.Comp.FreezeSound, husk);
    }

    // TODO: needs to live not here.
    private IEnumerable<EntityUid> GetRecursiveContainedEntities(Entity<ContainerManagerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            yield break;

        foreach (var container in _container.GetAllContainers(ent, ent))
        {
            if (container.ContainedEntities.Count == 0)
                continue;

            foreach (var contained in container.ContainedEntities)
            {
                yield return contained;

                foreach (var subContents in GetRecursiveContainedEntities(contained))
                {
                    yield return subContents;
                }
            }
        }
    }

    public override void Update(float frameTime)
    {
        foreach (var (uid, comp, xform) in EntityQueryEnumerator<ESCryohuskableComponent, TransformComponent>())
        {
            if (_timing.CurTime < comp.NextUpdate)
                continue;
            comp.NextUpdate += comp.UpdateRate;

            // Must be unconscious
            if (_actionBlocker.CanConsciouslyPerformAction(uid))
                continue;

            if (_atmosphere.GetTileMixture((uid, xform)) is not { } mix ||
                mix.GetMoles(Gas.Cryogas) < comp.MinConversionMols)
                continue;

            if (!_random.Prob(comp.ConversionChance))
                continue;

            Cryohusk(uid);
        }
    }
}

[ToolshedCommand, AdminCommand(AdminFlags.Fun)]
public sealed partial class ESCryohuskCommand : ToolshedCommand
{
    [Dependency] private IEntityManager _entityManager = default!;
    private ESCryohuskSystem? _cryohusk;

    [CommandImplementation("cryohusk")]
    public void Cryohusk([PipedArgument] EntityUid target)
    {
        _cryohusk ??= _entityManager.System<ESCryohuskSystem>();
        _cryohusk.Cryohusk(target);
    }
}
