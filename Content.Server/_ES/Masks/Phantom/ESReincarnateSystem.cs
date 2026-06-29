using System.Linq;
using Content.Server._ES.Objectives;
using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Shared._ES.Auditions.Components;
using Content.Shared._ES.Masks.Phantom.Components;
using Content.Shared._Offbrand.Wounds;
using Content.Shared.Mind;

namespace Content.Server._ES.Masks.Phantom;

public sealed partial class ESReincarnateSystem : EntitySystem
{
    [Dependency] private BrainDamageSystem _brain = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private ESObjectiveSystem _objective = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESReincarnateMindComponent, GhostAttemptHandleEvent>(OnGhostAttempt);
    }

    private void OnGhostAttempt(Entity<ESReincarnateMindComponent> ent, ref GhostAttemptHandleEvent args)
    {
        if (!TryComp<MindComponent>(ent, out var mindComp) ||
            !TryComp<ESCharacterComponent>(ent, out var character))
            return;

        if (_objective.GetObjectives<ESReincarnateObjectiveComponent>(ent.Owner)
            .Any(o => !_objective.IsCompleted(o.Owner)))
        {
            return;
        }

        if (HasComp<BrainDamageComponent>(mindComp.OwnedEntity))
        {
            _brain.KillBrain(mindComp.OwnedEntity.Value);
        }

        var coords = mindComp.OwnedEntity.HasValue
            ? Transform(mindComp.OwnedEntity.Value).Coordinates
            : _gameTicker.GetObserverSpawnPoint();

        var ghost = SpawnAtPosition(ent.Comp.ReincarnateEntity, coords);
        _metaData.SetEntityName(ghost, Loc.GetString(ent.Comp.Name, ("name", character.Name)));
        _mind.TransferTo(ent, ghost, createGhost: false, mind: mindComp);
        args.Result = true;
        args.Handled = true;

        RemCompDeferred(ent, ent.Comp);
    }
}
