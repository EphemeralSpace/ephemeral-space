using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Server.Mind;
using Content.Shared._ES.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server._ES.Masks.Objectives.Relays;

public sealed class ESMobStateRelaySystem : ESBaseMindRelay
{
    [Dependency] private readonly MindSystem _mind = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESMobStateRelayComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<ESMobStateRelayComponent> ent, ref MobStateChangedEvent args)
    {
        if (!_mind.TryGetMind(ent, out var mindId, out var mindComp))
            return;

        var ev = new ESMobStateChanged((args.Target, args.Component), args.OldMobState, args.NewMobState, args.Origin);
        RaiseMindEvent((mindId, mindComp), ref ev);
    }
}

/// <summary>
///     Raised directed on the mind when mob state changes.
/// </summary>
/// <see cref="MobStateChangedEvent"/>
[ByRefEvent]
public readonly record struct ESMobStateChanged(Entity<MobStateComponent> Mob, MobState OldMobState, MobState NewMobState, EntityUid? Origin = null);
