#nullable enable
using Content.Shared._ES.Core.Timer.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.IntegrationTests.Tests._ES.Timers;

/// <summary>
///     This is a pile of event listeners for <see cref="TimerTests.EnsureNullSpaceTimersDontBroadcast"/>
/// </summary>
public sealed class ESTimerTestSystem : EntitySystem
{
    public event Action? DirectedReceived;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<EnsureNullSpaceTimersDontBroadcastEvent>(HandleBroadcast);
        SubscribeLocalEvent<ESEnsureNullSpaceTimersDontBroadcastComponent, EnsureNullSpaceTimersDontBroadcastEvent>(HandleDirected);
    }

    private void HandleDirected(Entity<ESEnsureNullSpaceTimersDontBroadcastComponent> ent, ref EnsureNullSpaceTimersDontBroadcastEvent args)
    {
        DirectedReceived?.Invoke();
    }

    private void HandleBroadcast(EnsureNullSpaceTimersDontBroadcastEvent ev)
    {
        Assert.Fail("We received a broadcast event, this event is never broadcast.");
    }
}

[RegisterComponent]
public sealed partial class ESEnsureNullSpaceTimersDontBroadcastComponent : Component;

[NetSerializable, Serializable]
public sealed partial class EnsureNullSpaceTimersDontBroadcastEvent : ESEntityTimerEvent;
