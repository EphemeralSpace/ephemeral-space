using Content.Server._ES.Objectives;
using Content.Server._ES.Troupes.Parasite.Components;
using Content.Server.RoundEnd;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.Objectives.Components;

namespace Content.Server._ES.Troupes.Parasite;

public sealed class ESParasiteRuleSystem : EntitySystem
{
    [Dependency] private readonly ESEntityTimerSystem _entityTimer = default!;
    [Dependency] private readonly ESObjectiveSystem _objective = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESObjectiveProgressChangedEvent>(OnProgressChanged);
    }

    private void OnProgressChanged(ref ESObjectiveProgressChangedEvent args)
    {
        var query = EntityQueryEnumerator<ESParasiteRuleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.ObjectivesCompleted)
                continue;

            if (!_objective.AllCompleted(uid))
                continue;

            StartEndPhase((uid, comp));
        }
    }

    private void StartEndPhase(Entity<ESParasiteRuleComponent> ent)
    {
        ent.Comp.ObjectivesCompleted = true;

        _entityTimer.SpawnMethodTimer(ent.Comp.WinDelay, () => { ent.Comp.WinStarted = true; });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ESParasiteRuleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.WinStarted)
                continue;

            if (_objective.AllCompleted(uid))
                _roundEnd.EndRound();
        }
    }
}
