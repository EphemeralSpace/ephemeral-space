using Content.Server._ES.Stagehand;
using Content.Server._ES.StationVariation.Components;
using Content.Server.GameTicking.Rules;
using Content.Shared._ES.Lobby.Components;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Movement.Components;

namespace Content.Server._ES.StationVariation.Systems;

public sealed class ESRotatedCameraGameRuleSystem : GameRuleSystem<ESRotatedCameraGameRuleComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<ESStagehandSpawnedEvent>(OnStagehandSpawnComplete);
    }

    protected override void Started(EntityUid uid,
        ESRotatedCameraGameRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        // if we're added midround check for anyone thats already been spawned
        var query = EntityQueryEnumerator<InputMoverComponent>();
        while (query.MoveNext(out var entity, out var mover))
        {
            if (HasComp<ESTheatergoerMarkerComponent>(entity))
                continue;

            mover.TargetRelativeRotation += component.Angle;
            Dirty(entity, mover);
        }
    }

    protected override void Ended(EntityUid uid,
        ESRotatedCameraGameRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleEndedEvent args)
    {
        var query = EntityQueryEnumerator<InputMoverComponent>();
        while (query.MoveNext(out var entity, out var mover))
        {
            if (HasComp<ESTheatergoerMarkerComponent>(entity))
                continue;

            mover.TargetRelativeRotation -= component.Angle;
            Dirty(entity, mover);
        }
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (!TryComp<InputMoverComponent>(ev.Mob, out var mover))
            return;

        var ruleQuery = EntityQueryEnumerator<ESRotatedCameraGameRuleComponent>();
        while (ruleQuery.MoveNext(out _, out var rule))
        {
            mover.TargetRelativeRotation += rule.Angle;
        }
    }

    private void OnStagehandSpawnComplete(ref ESStagehandSpawnedEvent ev)
    {
        if (!TryComp<InputMoverComponent>(ev.Stagehand, out var mover))
            return;

        var ruleQuery = EntityQueryEnumerator<ESRotatedCameraGameRuleComponent>();
        while (ruleQuery.MoveNext(out _, out var rule))
        {
            mover.TargetRelativeRotation += rule.Angle;
        }
    }
}
