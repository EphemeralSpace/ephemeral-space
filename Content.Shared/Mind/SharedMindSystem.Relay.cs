using Content.Shared._ES.Objectives.Components;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Mind.Components;
// ES START
using Content.Shared.Mobs;
// ES END

namespace Content.Shared.Mind;

/// <summary>
///     Relays events raised on a mobs body to its mind and mind role entities.
///     Useful for events that should be raised both on the body and the mind.
/// </summary>
public abstract partial class SharedMindSystem : EntitySystem
{
    public void InitializeRelay()
    {
        // for name modifiers that depend on certain mind roles
        SubscribeLocalEvent<MindContainerComponent, RefreshNameModifiersEvent>(RelayRefToMind);

// ES PATCH START
        SubscribeLocalEvent<MindContainerComponent, MobStateChangedEvent>(RelayToMind);

        SubscribeLocalEvent<MindComponent, MindGotAddedEvent>(ESOnMindGotAdded);
        SubscribeLocalEvent<MindComponent, ESObjectivesChangedEvent>(ESOnObjectivesChanged);
    }

    private void ESOnMindGotAdded(Entity<MindComponent> ent, ref MindGotAddedEvent args)
    {
        RelayToObjectives(ent, ref args);
        foreach (var role in ent.Comp.MindRoleContainer.ContainedEntities)
        {
            RaiseLocalEvent(role, args);
        }
    }

    private void ESOnObjectivesChanged(Entity<MindComponent> ent, ref ESObjectivesChangedEvent args)
    {
        if (ent.Comp.OwnedEntity is { } owned)
            RaiseLocalEvent(owned, ref args);
    }

    protected void RelayToObjectives<T>(Entity<MindComponent> ent, ref T args) where T : notnull
    {
        foreach (var objective in ent.Comp.Objectives)
        {
            RaiseLocalEvent(objective, args);
        }
    }

    protected void RelayToMind<T>(EntityUid uid, MindContainerComponent component, T args) where T : notnull
// ES PATCH END
    {
        var ev = new MindRelayedEvent<T>(args);

        if (TryGetMind(uid, out var mindId, out var mindComp, component))
        {
            RaiseLocalEvent(mindId, ref ev);

            foreach (var role in mindComp.MindRoleContainer.ContainedEntities)
                RaiseLocalEvent(role, ref ev);
        }
    }

    protected void RelayRefToMind<T>(EntityUid uid, MindContainerComponent component, ref T args) where T : class
    {
        var ev = new MindRelayedEvent<T>(args);

        if (TryGetMind(uid, out var mindId, out var mindComp, component))
        {
            RaiseLocalEvent(mindId, ref ev);

            foreach (var role in mindComp.MindRoleContainer.ContainedEntities)
                RaiseLocalEvent(role, ref ev);
        }

        args = ev.Args;
    }
}

[ByRefEvent]
public record struct MindRelayedEvent<TEvent>(TEvent Args);
