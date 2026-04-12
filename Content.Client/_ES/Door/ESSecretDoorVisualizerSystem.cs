using Content.Client._ES.Door.Components;
using Content.Client.IconSmoothing;
using Content.Shared.Doors.Components;
using Robust.Client.GameObjects;

namespace Content.Client._ES.Door;

public sealed class ESSecretDoorVisualizerSystem : VisualizerSystem<ESSecretDoorVisualsComponent>
{
    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESSecretDoorVisualsComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    private void OnAnimationCompleted(Entity<ESSecretDoorVisualsComponent> ent, ref AnimationCompletedEvent args)
    {
        UpdateAppearance(ent.Owner);
    }

    protected override void OnAppearanceChange(EntityUid uid, ESSecretDoorVisualsComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);


        UpdateAppearance((uid, args.Sprite, args.Component));
    }

    private void UpdateAppearance(Entity<SpriteComponent?, AppearanceComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return;

        if (_animationPlayer.HasRunningAnimation(ent, DoorComponent.OpenCloseKey))
            return;

        if (!AppearanceSystem.TryGetData<DoorState>(ent, DoorVisuals.State, out var state, ent))
            state = DoorState.Closed;

        var hidden = state == DoorState.Closed;

        SpriteSystem.LayerSetVisible((ent, ent), DoorVisualLayers.Base, !hidden);
        SpriteSystem.LayerSetVisible((ent, ent), DoorVisualLayers.BaseBolted, !hidden);
        SpriteSystem.LayerSetVisible((ent, ent), DoorVisualLayers.BaseEmagging, !hidden);
        SpriteSystem.LayerSetVisible((ent, ent), DoorVisualLayers.BaseEmergencyAccess, !hidden);
        SpriteSystem.LayerSetVisible((ent, ent), DoorVisualLayers.BaseUnlit, !hidden);
        SpriteSystem.LayerSetVisible((ent, ent), IconSmoothSystem.CornerLayers.NE, hidden);
        SpriteSystem.LayerSetVisible((ent, ent), IconSmoothSystem.CornerLayers.NW, hidden);
        SpriteSystem.LayerSetVisible((ent, ent), IconSmoothSystem.CornerLayers.SE, hidden);
        SpriteSystem.LayerSetVisible((ent, ent), IconSmoothSystem.CornerLayers.SW, hidden);
    }
}
