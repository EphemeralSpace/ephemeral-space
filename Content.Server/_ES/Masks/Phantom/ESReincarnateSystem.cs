using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Shared._ES.Auditions.Components;
using Content.Shared._ES.Masks.Phantom.Components;
using Content.Shared._Offbrand.Wounds;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Phantom;

public sealed class ESReincarnateSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly BrainDamageSystem _brain = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;

    private static readonly ProtoId<DamageTypePrototype> AsphyxiationDamageType = "Asphyxiation";

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
