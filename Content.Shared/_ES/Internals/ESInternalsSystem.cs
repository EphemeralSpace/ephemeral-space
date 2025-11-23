using Content.Shared.ActionBlocker;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Interaction;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Internals;

public sealed class ESInternalsSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedGasTankSystem _internals = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeAllEvent<ESToggleInternalsEvent>(OnToggleInternals);
    }

    private void OnToggleInternals(ESToggleInternalsEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } ent ||
            !TryGetEntity(msg.Tank, out var tank) ||
            !TryComp<GasTankComponent>(tank, out var tankComp))
            return;

        if (!_interaction.IsAccessible(ent, tank.Value) ||
            !_actionBlocker.CanInteract(ent, tank.Value))
            return;

        _internals.ToggleInternals((tank.Value, tankComp), ent);
    }
}

[Serializable, NetSerializable]
public sealed class ESToggleInternalsEvent : EntityEventArgs
{
    public NetEntity Tank;

    public ESToggleInternalsEvent(NetEntity tank)
    {
        Tank = tank;
    }
}
