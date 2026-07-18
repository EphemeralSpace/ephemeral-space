using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Components;

namespace Content.Client._ES.Objectives;

/// <inheritdoc/>
public sealed class ESObjectiveSystem : ESSharedObjectiveSystem
{
    public event Action<Entity<ESObjectiveHolderComponent>>? OnObjectivesChanged;
    public event Action<Entity<ESObjectiveComponent>>? OnObjectiveProgressChanged;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESObjectiveHolderComponent, AfterAutoHandleStateEvent>(OnHolderAfterAutoHandleStateEvent);
        SubscribeLocalEvent<ESObjectiveComponent, AfterAutoHandleStateEvent>(OnObjectiveAfterAutoHandleState);
        SubscribeLocalEvent<ESObjectiveDescriptorComponent, AfterAutoHandleStateEvent>(OnDescriptorAfterAutoHandleState);
    }

    private void OnHolderAfterAutoHandleStateEvent(Entity<ESObjectiveHolderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        OnObjectivesChanged?.Invoke(ent);
    }

    private void OnObjectiveAfterAutoHandleState(Entity<ESObjectiveComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        OnObjectiveProgressChanged?.Invoke(ent);
    }

    private void OnDescriptorAfterAutoHandleState(Entity<ESObjectiveDescriptorComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (TryComp<ESObjectiveComponent>(ent, out var comp))
            OnObjectiveProgressChanged?.Invoke((ent, comp));
    }
}
