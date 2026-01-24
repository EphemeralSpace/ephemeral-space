using System.Numerics;
using Content.Server._ES.StationEvents.Meteors.Components;
using Content.Server.StationEvents.Events;
using Content.Shared._ES.Core;
using Content.Shared._ES.Voting.Components;
using Content.Shared._ES.Voting.Results;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Content.Shared.Localizations;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Server._ES.StationEvents.Meteors;

public sealed class ESMeteorsRule : StationEventSystem<ESMeteorsRuleComponent>
{
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESAngleVoteComponent, ESGetVoteOptionsEvent>(OnGetVoteOptions);
        SubscribeLocalEvent<ESMeteorsRuleComponent, ESSynchronizedVotesCompletedEvent>(OnVotesCompleted);
    }

    private void OnGetVoteOptions(Entity<ESAngleVoteComponent> ent, ref ESGetVoteOptionsEvent args)
    {
        for (var i = 0; i < ent.Comp.Count; i++)
        {
            var angle = RobustRandom.NextAngle();
            var readableAngle = angle.ToCompassAngle();
            args.Options.Add(new ESAngleVoteOption
            {
                Angle = angle,
                DisplayString = Loc.GetString("es-angle-vote-option-fmt", ("dir", ContentLocalizationManager.FormatDirection(angle.GetDir())), ("angle", (int) readableAngle.Degrees)),
            });
        }
    }

    private void OnVotesCompleted(Entity<ESMeteorsRuleComponent> ent, ref ESSynchronizedVotesCompletedEvent args)
    {
        if (!args.TryGetResult<ESAngleVoteOption>(0, out var angle))
            return;

        ent.Comp.LaunchAngle = angle.Angle;
        SetStartAnnouncement(ent.Owner, Loc.GetString("es-station-event-meteor-swarm-start-announcement", ("dir", ContentLocalizationManager.FormatDirection(angle.Angle.GetDir()).ToLowerInvariant())));
    }

    protected override void Started(EntityUid uid, ESMeteorsRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        component.NextWaveTime = Timing.CurTime;
    }

    protected override void ActiveTick(EntityUid uid, ESMeteorsRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        if (Timing.CurTime < component.NextWaveTime)
            return;
        component.NextWaveTime += RobustRandom.Next(component.MinWaveDelay, component.MaxWaveDelay);

        if (!TryGetRandomStation(out var station) ||
            StationSystem.GetLargestGrid(station.Value) is not { } grid)
            return;

        var xform = Transform(grid);
        var mapId = xform.MapID;

        var gridAABB = _physics.GetWorldAABB(grid);
        var gridHalfExtent = gridAABB.Extents.Length() / 2;

        foreach (var spawnProto in _entityTable.GetSpawns(component.MeteorTable))
        {
            var angle = (xform.LocalRotation + component.LaunchAngle.Opposite() - MathF.PI / 2).Opposite();

            var distanceOffset = MathHelper.Lerp(gridHalfExtent + 100, gridHalfExtent + 150, RobustRandom.NextFloat());
            var lateralOffset = gridAABB.MinDimension() * RobustRandom.NextFloat(-1, 1) / 2;
            var offset = angle.RotateVec(new Vector2(distanceOffset, lateralOffset));

            var spawnPosition = new MapCoordinates(gridAABB.Center + offset, mapId);
            var meteor = Spawn(spawnProto, spawnPosition);
            var physics = Comp<PhysicsComponent>(meteor);
            _physics.ApplyLinearImpulse(meteor, -angle.ToVec() * component.MeteorVelocity * physics.Mass, body: physics);
        }

        if (--component.Waves <= 0)
            ForceEndSelf(uid, gameRule);
    }
}
