using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Humanoid;
using Content.Server.Mind;
using Content.Server.Speech.Components;
using Content.Shared._ES.Cryohusk;
using Content.Shared._ES.Cryohusk.Components;
using Content.Shared._ES.Stagehand;
using Content.Shared.Access.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration;
using Content.Shared.Administration.Systems;
using Content.Shared.Atmos;
using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Preferences;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
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
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private RejuvenateSystem _rejuvenate = default!;
    [Dependency] private ESSharedStagehandNotificationsSystem _stagehandNotifications = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;

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
        if (!Resolve(target, ref target.Comp, false))
            return;

        if (_mind.TryGetMind(target, out _) && !_mobState.IsDead(target))
        {
            var msg = Loc.GetString("es-cryohusk-convert-stagehand-notif",
                ("player", _stagehandNotifications.WrapEntityName(target.Owner)));
            _stagehandNotifications.SendStagehandNotification(msg);
        }

        _metaData.SetEntityName(target, Loc.GetString("es-cryohusk-name"), raiseEvents: false);

        var evt = new ESGotCryohuskedEvent();
        RaiseLocalEvent(target, ref evt);

        var profile = new HumanoidCharacterProfile()
            .WithGender(Gender.Neuter)
            .WithSex(Sex.Unsexed)
            .WithSpecies(target.Comp.CryohuskSpecies)
            .WithCharacterAppearance(
                new HumanoidCharacterAppearance()
                    .WithSkinColor(Color.White));

        _humanoidProfile.ApplyProfileTo(target.Owner, profile);
        _visualBody.ApplyProfileTo(target.Owner, profile);

        foreach (var uid in GetRecursiveContainedEntities(target.Owner))
        {
            if (_idCardQuery.HasComp(uid))
                EnsureComp<ESCryohuskIdCardComponent>(uid);
        }

        _audio.PlayPvs(target.Comp.FreezeSound, target);

        EnsureComp<SlurredAccentComponent>(target);

        _damageable.SetDamageModifierSetId(target.Owner, target.Comp.DamageModifierSet);

        // No double husking
        EnsureComp<ESCryohuskComponent>(target);
        RemComp<ESCryohuskableComponent>(target);
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
        base.Update(frameTime);

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

            // cryohusking a non-dead target is a full heal...
            if (!_mobState.IsDead(uid))
                _rejuvenate.PerformRejuvenate(uid);
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
