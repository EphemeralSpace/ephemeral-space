using Content.Shared.Mind.Components;
using Robust.Shared.Player;

namespace Content.Shared._ES.Mind;

public sealed class ESPlayerStatusRelaySystem : ESBaseMindRelay
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindContainerComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<MindContainerComponent, PlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnPlayerAttached(Entity<MindContainerComponent> ent, ref PlayerAttachedEvent args)
    {
        if (!TryGetMind(ent, out var mind))
            return;

        var ev = new ESMindPlayerAttachedEvent(args.Entity, args.Player);
        RaiseMindEvent(mind.Value, ref ev);
    }

    private void OnPlayerDetached(Entity<MindContainerComponent> ent, ref PlayerDetachedEvent args)
    {
        if (!TryGetMind(ent, out var mind))
            return;

        var ev = new ESMindPlayerDetachedEvent(args.Entity, args.Player);
        RaiseMindEvent(mind.Value, ref ev);
    }
}

/// <summary>
/// Event raised on a mind and objectives when a player is attached to a body.
/// </summary>
[ByRefEvent]
public readonly record struct ESMindPlayerAttachedEvent(EntityUid Entity, ICommonSession Player);

/// <summary>
/// Event raised on a mind and objectives when a player is detached from a body.
/// </summary>
[ByRefEvent]
public readonly record struct ESMindPlayerDetachedEvent(EntityUid Entity, ICommonSession Player);
