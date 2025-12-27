using Content.Shared._ES.Telesci;
using Content.Shared._ES.Telesci.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client._ES.Telesci;

public sealed class ESTelesciSystem : ESSharedTelesciSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const string RewardPadAnimKey = "reward-pad-anim";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ESAnimateTelesciRewardPadMessage>(OnAnimateRewardPad);
    }

    private void OnAnimateRewardPad(ESAnimateTelesciRewardPadMessage args)
    {
        if (!TryGetEntity(args.Pad, out var pad) ||
            !TryComp<ESTelesciRewardPadComponent>(pad, out var component) ||
            !TryComp<SpriteComponent>(pad, out var sprite))
            return;

        if (_animationPlayer.HasRunningAnimation(pad.Value, RewardPadAnimKey))
            return;

        if (!_sprite.TryGetLayer((pad.Value, sprite), component.TeleportKey, out var layer, true))
            return;

        if (layer.ActualRsi == null ||
            !layer.ActualRsi.TryGetState(component.TeleportState, out var teleportState))
            return;

        var baseState = layer.State;

        var anim = new Animation
        {
            Length = TimeSpan.FromSeconds(teleportState.AnimationLength),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = component.TeleportKey,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(new RSI.StateId(component.TeleportState), 0f),
                        new AnimationTrackSpriteFlick.KeyFrame(baseState, teleportState.AnimationLength),
                    },
                },
            }
        };
        _animationPlayer.Play(pad.Value, anim, RewardPadAnimKey);
    }
}
