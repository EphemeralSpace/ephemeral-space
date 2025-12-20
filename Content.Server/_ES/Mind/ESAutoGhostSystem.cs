using Content.Server.Ghost;
using Content.Server.KillTracking;
using Content.Server.Mind;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.Mind;
using Content.Shared.Ghost;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;

namespace Content.Server._ES.Mind;

/// <summary>
/// Handles automatically ghosting the player and removing their mind when they die.
/// </summary>
public sealed class ESAutoGhostSystem : EntitySystem
{
    [Dependency] private readonly ESEntityTimerSystem _entityTimer = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private static readonly TimeSpan AutoGhostDelay = TimeSpan.FromSeconds(1);

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GhostOnMoveComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<GhostOnMoveComponent, MobStateChangedEvent>(OnMobStateChanged, after: [typeof(KillTrackingSystem)]);

        SubscribeLocalEvent<MindContainerComponent, ESAutoGhostEvent>(OnAutoGhost);
    }

    private void OnStartup(Entity<GhostOnMoveComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.MustBeDead && !_mobState.IsDead(ent))
            return;

        AutoGhost(ent);
    }

    private void OnMobStateChanged(Entity<GhostOnMoveComponent> ent, ref MobStateChangedEvent args)
    {
        // Only ghost when dead
        if (args.NewMobState != MobState.Dead)
            return;

        AutoGhost(ent);
    }

    private void OnAutoGhost(Entity<MindContainerComponent> ent, ref ESAutoGhostEvent args)
    {
        if (!_mind.TryGetMind(ent, out var mindId, out var mindComp, ent))
            return;

        _ghost.OnGhostAttempt(mindId, canReturnGlobal: false, forced: true, mind: mindComp);
    }

    private void AutoGhost(EntityUid uid)
    {
        _entityTimer.SpawnTimer(uid, AutoGhostDelay, new ESAutoGhostEvent());
    }
}
