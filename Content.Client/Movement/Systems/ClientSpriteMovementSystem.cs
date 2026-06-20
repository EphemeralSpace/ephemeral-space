using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Robust.Client.GameObjects;

namespace Content.Client.Movement.Systems;

/// <summary>
/// Controls the switching of motion and standing still animation
/// </summary>
public sealed class ClientSpriteMovementSystem : VisualizerSystem<SpriteMovementComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, SpriteMovementComponent comp, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;
        if (!args.AppearanceData.TryGetValue(SpriteMovementVisuals.Moving, out var obj) || obj is not bool isMoving)
            return;

        comp.WasMoving ??= !isMoving;
        if (isMoving == comp.WasMoving)
            return;

        void SetLayers(Dictionary<string, PrototypeLayerData> layers)
        {
            foreach (var (layer, state) in layers)
            {
                if (!SpriteSystem.TryGetLayer((uid, sprite), layer, out var layerData, true))
                    continue;

                var oldTime = layerData.AnimationTime;
                var oldStateWasAnim = layerData.AnimationTimeLeft > 0;
                SpriteSystem.LayerSetAutoAnimated(layerData, true);
                SpriteSystem.LayerSetData(layerData, state);
                // if there was old anim time left from a previously playing anim, take that into account here
                if (oldStateWasAnim)
                {
                    var setAnimTime = layerData.AnimationTimeLeft - oldTime;
                    SpriteSystem.LayerSetAnimationTime(layerData, setAnimTime);
                }
            }
        }

        Log.Info($"setting layers {isMoving}");
        SetLayers(isMoving ? comp.MovementLayers : comp.NoMovementLayers);
        comp.WasMoving = isMoving;
    }
}
