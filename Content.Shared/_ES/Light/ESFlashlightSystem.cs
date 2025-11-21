using Content.Shared.ActionBlocker;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Light;

public sealed class ESFlashlightSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedHandheldLightSystem _handheldLight = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly UnpoweredFlashlightSystem _unpoweredFlashlight = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<UnpoweredFlashlightComponent, ExaminedEvent>(OnExamine);
        
        SubscribeAllEvent<ESToggleFlashlightEvent>(OnToggleFlashlight);
    }

    private void OnExamine(Entity<UnpoweredFlashlightComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("es-flashlight-toggle-examine-keybind"));
    }

    private void OnToggleFlashlight(ESToggleFlashlightEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } ent ||
            !TryGetEntity(msg.Flashlight, out var flashlight))
            return;

        if (!_interaction.IsAccessible(ent, flashlight.Value) ||
            !_actionBlocker.CanInteract(ent, flashlight.Value))
            return;

        _unpoweredFlashlight.TryToggleLight(flashlight.Value, ent);
        _handheldLight.TryToggleLight(flashlight.Value, ent);
    }
}

[Serializable, NetSerializable]
public sealed class ESToggleFlashlightEvent : EntityEventArgs
{
    public NetEntity Flashlight;

    public ESToggleFlashlightEvent(NetEntity flashlight)
    {
        Flashlight = flashlight;
    }
}
