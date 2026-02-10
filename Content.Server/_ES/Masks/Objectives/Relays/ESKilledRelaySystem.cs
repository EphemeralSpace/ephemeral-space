using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Server.KillTracking;
using Content.Server.Mind;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.Mind;

namespace Content.Server._ES.Masks.Objectives.Relays;

public sealed class ESKilledRelaySystem : ESBaseMindRelay
{
    [Dependency] private readonly MindSystem _mind = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESKilledRelayComponent, ESPlayerKilledEvent>(OnKillReported);
    }

    private void OnKillReported(Entity<ESKilledRelayComponent> ent, ref ESPlayerKilledEvent args)
    {
        if (!_mind.TryGetMind(ent, out var mindId, out var mindComp))
            return;

        RaiseMindEvent((mindId, mindComp), ref args);
    }
}
