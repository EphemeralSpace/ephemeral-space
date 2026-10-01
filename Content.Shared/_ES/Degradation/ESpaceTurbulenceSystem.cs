using Content.Shared._ES.Camera;
using Content.Shared._ES.Degradation.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Station;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Shared._ES.Degradation;

public sealed partial class ESpaceTurbulenceSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ESScreenshakeSystem _screenshake = default!;
    [Dependency] private SharedStationSystem _station = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private ThrowingSystem _throwing = default!;

    [Dependency] private EntityQuery<BuckleComponent> _buckleQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESSpaceTurbulenceEventComponent, ESSpaceTurbulenceJerkTimerEvent>(OnSpaceTurbulenceJerkTimer);
    }

    private void OnSpaceTurbulenceJerkTimer(Entity<ESSpaceTurbulenceEventComponent> ent, ref ESSpaceTurbulenceJerkTimerEvent args)
    {
        var throwAngle = _random.NextAngle();
        var throwVec = throwAngle.ToVec() * ent.Comp.ThrowDistance;

        foreach (var station in _station.GetStations())
        {
            // We only care about the main grid T B H
            if (_station.GetLargestGrid(station) is not { } grid)
                continue;

            var gridTransform = Transform(grid);

            _screenshake.Screenshake(Filter.BroadcastGrid(grid), new ESScreenshakeParameters(0.6f, 0.8f), null);

            var enumerator = gridTransform.ChildEnumerator;
            while (enumerator.MoveNext(out var uid))
            {
                if (!_buckleQuery.TryComp(uid, out var buckle) || buckle.Buckled)
                    continue;

                _stun.TryUpdateParalyzeDuration(uid, ent.Comp.StunTime);
                _throwing.TryThrow(uid, throwVec, baseThrowSpeed: ent.Comp.ThrowForce);
            }
        }
    }
}
