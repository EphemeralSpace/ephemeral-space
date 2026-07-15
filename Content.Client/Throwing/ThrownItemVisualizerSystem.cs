using Content.Shared.Throwing;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client.Throwing;

/// <summary>
///     Handles animating thrown items.
/// </summary>
public sealed partial class ThrownItemVisualizerSystem : EntitySystem
{
    [Dependency] private AnimationPlayerSystem _anim = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private const string AnimationKey = "thrown-item";

    /// <summary>
    ///     Amount of spins per second of airtime.
    /// </summary>
    private const float ThrowSpinPerSecond = 0.28f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThrownItemComponent, AfterAutoHandleStateEvent>(OnAutoHandleState);
        SubscribeLocalEvent<ThrownItemComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnAutoHandleState(EntityUid uid, ThrownItemComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !component.Animate)
            return;

        var animationPlayer = EnsureComp<AnimationPlayerComponent>(uid);

        if (_anim.HasRunningAnimation(uid, animationPlayer, AnimationKey))
            return;

        var anim = GetAnimation((uid, component, sprite));
        if (anim == null)
            return;

        component.OriginalScale = sprite.Scale;
        _anim.Play((uid, animationPlayer), anim, AnimationKey);
    }

    private void OnShutdown(EntityUid uid, ThrownItemComponent component, ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite) && component is { OriginalScale: not null })
        {
            _sprite.SetScale((uid, sprite), component.OriginalScale.Value);
            _sprite.SetRotation((uid, sprite), Angle.Zero);
        }

        _anim.Stop(uid, AnimationKey);
    }

    private Animation? GetAnimation(Entity<ThrownItemComponent, SpriteComponent> ent)
    {
        if (ent.Comp1.LandTime - ent.Comp1.ThrownTime is not { } length)
            return null;

        if (length <= TimeSpan.Zero)
            return null;

        var scale = ent.Comp2.Scale;
        var lenFloat = (float)length.TotalSeconds;

        var anim = new Animation
        {
            Length = length,
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    KeyFrames =
                    {
                        // ES START
                        new AnimationTrackProperty.KeyFrame(scale, 0.0f),
                        new AnimationTrackProperty.KeyFrame(scale * 1.4f, lenFloat * 0.5f, Easings.OutQuad),
                        new AnimationTrackProperty.KeyFrame(scale, lenFloat * 0.5f, Easings.InQuad)
                        // ES END
                    },
                    InterpolationMode = AnimationInterpolationMode.Linear,
                },
            },
        };

        // early-out if this is an item with a throwing angle (dont do rotation anim)
        if (HasComp<ThrowingAngleComponent>(ent))
            return anim;

        // throw rotation anim

        // We step the amount of 'full spins' according to throw time
        // and only do an integer amount of spins, always ending on 0 rotation
        // (we want to avoid arbitrarily rotated items where possible for readability reasons)
        var spins = (int)MathF.Floor(lenFloat / ThrowSpinPerSecond);
        var rotationKeyframes = new List<AnimationTrackProperty.KeyFrame>();
        rotationKeyframes.Add(new AnimationTrackProperty.KeyFrame(Angle.Zero, 0.0f));
        for (var i = 0; i < spins; i++)
        {
            var angleHalf = new Angle(Math.PI);
            var angleFull = new Angle(Math.PI * 2);
            var timeHalf = ThrowSpinPerSecond * (i + 0.5f);
            var timeFull = ThrowSpinPerSecond * (i + 1);
            rotationKeyframes.Add(new AnimationTrackProperty.KeyFrame(angleHalf, timeHalf));
            rotationKeyframes.Add(new AnimationTrackProperty.KeyFrame(angleFull, timeFull));
            // get around going from 360->180 not reducing and going backwards
            rotationKeyframes.Add(new AnimationTrackProperty.KeyFrame(Angle.Zero, timeFull));
        }

        anim.AnimationTracks.Add(new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(SpriteComponent),
                Property = nameof(SpriteComponent.Rotation),
                KeyFrames = rotationKeyframes,
                InterpolationMode = AnimationInterpolationMode.Linear,
            });

        return anim;
    }
}
