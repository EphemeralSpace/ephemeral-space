using Content.Shared._ES.Keypad;
using Content.Shared._ES.Keypad.Components;
using Content.Shared.Power;
using Robust.Shared.Timing;

namespace Content.Client._ES.Keypad;

/// <inheritdoc/>
public sealed class ESKeypadSystem : ESSharedKeypadSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public event Action<Entity<ESKeypadComponent>>? OnCurrentCodeUpdated;
    public event Action<Entity<ESKeypadComponent>>? OnLockedUpdated;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESKeypadComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
        SubscribeLocalEvent<ESKeypadComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnAfterAutoHandleState(Entity<ESKeypadComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (args.State is not ESKeypadComponent.ESKeypadComponent_AutoState)
            return;

        // Causes mispredicts
        if (!_timing.IsFirstTimePredicted)
            return;

        OnCurrentCodeUpdated?.Invoke(ent);
        OnLockedUpdated?.Invoke(ent);
    }

    private void OnPowerChanged(Entity<ESKeypadComponent> ent, ref PowerChangedEvent args)
    {
        OnCurrentCodeUpdated?.Invoke(ent);
        OnLockedUpdated?.Invoke(ent);
    }
}
