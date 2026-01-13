using Content.Shared._ES.Objectives.Target.Components;

namespace Content.Shared._ES.Objectives.Target;

/// <summary>
/// Variant of <see cref="ESBaseObjectiveSystem{TComponent}"/> for specific integration with <see cref="ESTargetObjectiveComponent"/>
/// </summary>
public abstract class ESBaseTargetObjectiveSystem<TComponent> : ESBaseObjectiveSystem<TComponent>
    where TComponent : Component
{
    /// <summary>
    /// A list of all relays present on the target that this objective relies on existing
    /// </summary>
    /// <remarks>
    /// Essentially, when a target is selected, all of these components will be automatically added to them.
    /// This allows you to run specific logic on them without needing to broadly query or do global subs.
    /// </remarks>
    public virtual Type[] TargetRelayComponents => Array.Empty<Type>();

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TComponent, ESObjectiveTargetChangedEvent>(OnTargetChanged);
    }

    [MustCallBase]
    protected virtual void OnTargetChanged(Entity<TComponent> ent, ref ESObjectiveTargetChangedEvent args)
    {
        if (args.OldTarget is { } oldTarget)
        {
            foreach (var relayType in TargetRelayComponents)
            {
                if (HasComp(oldTarget, relayType))
                    RemComp(oldTarget, Factory.GetComponent(relayType));
            }
        }

        if (args.NewTarget is { } newTarget)
        {
            foreach (var relayType in TargetRelayComponents)
            {
                if (!HasComp(newTarget, relayType))
                    AddComp(newTarget, Factory.GetComponent(relayType));
            }
        }
    }
}
