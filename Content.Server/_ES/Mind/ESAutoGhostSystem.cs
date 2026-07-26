using Content.Server.Ghost;
using Content.Server.KillTracking;
using Content.Server.Mind;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.DeathCutscene;
using Content.Shared._ES.Mind;
using Content.Shared._ES.Viewcone.Components;
using Content.Shared.Ghost;
using Content.Shared.Gibbing;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.Server._ES.Mind;

/// <summary>
/// Handles automatically ghosting the player and removing their mind when they die.
/// </summary>
public sealed partial class ESAutoGhostSystem : EntitySystem
{
    [Dependency] private ESEntityTimerSystem _entityTimer = default!;
    [Dependency] private GhostSystem _ghost = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MetaDataSystem _metadata = default!;
    [Dependency] private IPlayerManager _player = default!;

    // time for death cutscene to play out
    private static readonly TimeSpan AutoGhostDelay = TimeSpan.FromSeconds(15.5);

    /// <inheritdoc/>
    public override void Initialize()
    {
        // todo call this some other shit and makle it more generic see the comment in esbasemob
        SubscribeLocalEvent<GhostOnMoveComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<GhostOnMoveComponent, BeingGibbedEvent>(OnBeingGibbed);
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

    private void OnBeingGibbed(Entity<GhostOnMoveComponent> ent, ref BeingGibbedEvent args)
    {
        if (!TryComp<ActorComponent>(ent.Owner, out var actor) || !_mind.TryGetMind(ent.Owner, out var mind))
            return;

        // create dummy entity to attach plyer to
        var dummy = SpawnAtPosition(null, Transform(ent.Owner).Coordinates);
        _metadata.SetEntityName(dummy, $"gib dummy entity ({Name(ent.Owner)})");
        _mind.TransferTo(mind.Value.Owner, dummy);
        _player.SetAttachedEntity(actor.PlayerSession, dummy);

        AutoGhost(dummy);
    }

    private void OnAutoGhost(Entity<MindContainerComponent> ent, ref ESAutoGhostEvent args)
    {
        if (!_mind.TryGetMind(ent, out var mindId, out var mindComp, ent))
            return;

        _ghost.OnGhostAttempt(mindId, canReturnGlobal: false, forced: true, mind: mindComp);
    }

    private void AutoGhost(EntityUid uid)
    {
        // Don't ghost the brainless.
        if (!_mind.TryGetMind(uid, out _, out _))
            return;

        var ev = new AutoGhostAttemptEvent(uid);
        RaiseLocalEvent(uid, ref ev, true);
        if (ev.Cancelled)
            return;

        _entityTimer.SpawnTimer(uid, AutoGhostDelay, new ESAutoGhostEvent());

        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        // uhh teehee
        RemCompDeferred<ESViewconeComponent>(uid);
        RaiseNetworkEvent(new ESPlayDeathCutsceneNetworkEvent(), actor.PlayerSession);
    }
}

/// <summary>
///     Raised directed and broadcast to check if autoghost + the death cutscene should happen.
/// </summary>
[ByRefEvent]
public record struct AutoGhostAttemptEvent(EntityUid User, bool Cancelled = false);
