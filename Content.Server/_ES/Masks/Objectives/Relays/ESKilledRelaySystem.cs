using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Server.KillTracking;
using Content.Server.Mind;
using Content.Shared._ES.Mind;

namespace Content.Server._ES.Masks.Objectives.Relays;

public sealed class ESKilledRelaySystem : ESBaseMindRelay
{
    [Dependency] private readonly MindSystem _mind = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESKilledRelayComponent, KillReportedEvent>(OnKillReported);
    }

    private void OnKillReported(Entity<ESKilledRelayComponent> ent, ref KillReportedEvent args)
    {
        if (!_mind.TryGetMind(ent, out var mindId, out var mindComp))
            return;

        var ev = new ESKillReportedEvent(args.Entity, args.Primary, args.Assist, args.Suicide);
        RaiseMindEvent((mindId, mindComp), ref ev);
    }
}

/// <summary>
///     Raised directed on the mind when the body has been reported killed.
/// </summary>
[ByRefEvent]
public readonly record struct ESKillReportedEvent(EntityUid Entity, KillSource Primary, KillSource? Assist, bool Suicide);
