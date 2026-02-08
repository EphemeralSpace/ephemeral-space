using System.Numerics;
using Content.Shared._ES.Telesci.Anomaly;
using Content.Shared._ES.Telesci.Anomaly.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client._ES.Telesci.Anomaly;

/// <inheritdoc/>
public sealed class ESAnomalySystem : ESSharedAnomalySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;

    private const string AnimationKey = "es-anomaly-anim";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ESAnomalyCollapseAnimationEvent>(OnCollapseAnimation);
        SubscribeNetworkEvent<ESAnomalyShrinkAnimationEvent>(OnShrinkAnimation);
        SubscribeNetworkEvent<ESAnomalyRadiationAnimationEvent>(OnRadiationAnimation);
    }

    private void OnCollapseAnimation(ESAnomalyCollapseAnimationEvent ev)
    {
        if (!TryGetEntity(ev.Anomaly, out var uid) ||
            !TryComp<ESPortalAnomalyComponent>(uid, out var comp))
            return;

        if (_animationPlayer.HasRunningAnimation(uid.Value, AnimationKey))
            return;

        var anim = new Animation
        {
            Length = TimeSpan.FromSeconds(5),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Vector2.One, 0.0f),
                        new AnimationTrackProperty.KeyFrame(Vector2.One * 1.15f, 0.33f, Easings.OutCubic),
                        new AnimationTrackProperty.KeyFrame(Vector2.One * 0.1f, 5.00f, Easings.OutCirc),
                    },
                },
            },
        };
        _animationPlayer.Play(uid.Value, anim, AnimationKey);
    }

    private void OnShrinkAnimation(ESAnomalyShrinkAnimationEvent ev)
    {
        if (!TryGetEntity(ev.Anomaly, out var uid) ||
            !TryComp<ESPortalAnomalyComponent>(uid, out var comp))
            return;

        if (_animationPlayer.HasRunningAnimation(uid.Value, AnimationKey))
            return;

        var anim = new Animation
        {
            Length = TimeSpan.FromSeconds(2),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Vector2.One, 0.0f),
                        new AnimationTrackProperty.KeyFrame(Vector2.One * 1.15f, 0.1f, Easings.OutBack),
                        new AnimationTrackProperty.KeyFrame(Vector2.One * 0.55f, 0.5f, Easings.InOutSine),
                        new AnimationTrackProperty.KeyFrame(Vector2.One, 1.75f, Easings.OutCirc),
                    },
                },
            },
        };
        _animationPlayer.Play(uid.Value, anim, AnimationKey);
    }

    private void OnRadiationAnimation(ESAnomalyRadiationAnimationEvent ev)
    {
        if (!TryGetEntity(ev.Anomaly, out var uid) ||
            !TryComp<ESPortalAnomalyComponent>(uid, out var comp))
            return;

        if (_animationPlayer.HasRunningAnimation(uid.Value, AnimationKey))
            return;

        var anim = new Animation
        {
            Length = TimeSpan.FromSeconds(2),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Vector2.One, 0.0f),
                        new AnimationTrackProperty.KeyFrame(Vector2.One * 1.33f, 0.5f, Easings.InOutCubic),
                        new AnimationTrackProperty.KeyFrame(Vector2.One, 1.75f, Easings.OutSine),
                    },
                },
            },
        };
        _animationPlayer.Play(uid.Value, anim, AnimationKey);
    }
}
