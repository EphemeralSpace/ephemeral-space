using System.Numerics;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stunnable;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Random;

namespace Content.Client.Stunnable;

public sealed partial class StunSystem : SharedStunSystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private AnimationPlayerSystem _animation = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SpriteSystem _spriteSystem = default!;

    private const string FatigueAnimationKey = "fatigue";

    private readonly int[] _sign = [-1, 1];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StunVisualsComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<StunVisualsComponent, AppearanceChangeEvent>(OnAppearanceChanged);
        SubscribeLocalEvent<StunVisualsComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    /// <summary>
    ///     Add stun visual layers
    /// </summary>
    private void OnComponentInit(Entity<StunVisualsComponent> entity, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(entity, out var sprite))
            return;

        var spriteEntity = (entity.Owner, sprite);

        _spriteSystem.LayerMapReserve(spriteEntity, StunVisualLayers.StamCrit);
        _spriteSystem.LayerSetVisible(spriteEntity, StunVisualLayers.StamCrit, false);
        _spriteSystem.LayerSetOffset(spriteEntity, StunVisualLayers.StamCrit, new Vector2(0, 0.3125f));

        _spriteSystem.LayerSetRsi(spriteEntity, StunVisualLayers.StamCrit, entity.Comp.StarsPath);

        UpdateAppearance((entity, sprite), entity.Comp.State);
    }

    private void OnAppearanceChanged(Entity<StunVisualsComponent> entity, ref AppearanceChangeEvent args)
    {
        if (args.Sprite != null)
            UpdateAppearance((entity, args.Sprite), entity.Comp.State);
    }

    private void OnAnimationCompleted(Entity<StunVisualsComponent> ent, ref AnimationCompletedEvent args)
    {
        if (args.Key != FatigueAnimationKey)
            return;

        _spriteSystem.SetOffset(ent.Owner, ent.Comp.StartingOffset);
        UpdateAppearance((ent, null, ent.Comp), ent.Comp.State);
    }

    private void UpdateAppearance(Entity<SpriteComponent?, StunVisualsComponent?> entity, string state)
    {
        if (!Resolve(entity, ref entity.Comp1, ref entity.Comp2))
            return;

        if (_spriteSystem.LayerMapTryGet((entity, entity.Comp1), StunVisualLayers.StamCrit, out var index, false))
        {
            var visible = Appearance.TryGetData<bool>(entity, StunVisuals.SeeingStars, out var stars) && stars;

            _spriteSystem.LayerSetVisible((entity, entity.Comp1), index, visible);
            _spriteSystem.LayerSetRsiState((entity, entity.Comp1), index, state);
        }

        if (Appearance.TryGetData<bool>(entity, StunVisuals.Fatigue, out var fatigue) &&
            fatigue &&
            !_animation.HasRunningAnimation(entity, FatigueAnimationKey) &&
            !_mobState.IsDead(entity))
        {
            entity.Comp2.StartingOffset = entity.Comp1.Offset;
            var anim = GetFatigueAnimation(entity.Comp1,
                2,
                4,
                0.04f * Vector2.Create(0.5f, 0.125f),
                0.04f * Vector2.Create(1f, 0.25f),
                0.08f,
                entity.Comp1.Offset,
                ref entity.Comp2.LastJitter);

            _animation.Play(entity, anim, FatigueAnimationKey);
        }
    }

    /// <summary>
    /// A simple fatigue animation, a mild modification of the jittering animation. The animation constructor is
    /// quite complex, but that's because the AnimationSystem doesn't have proper adjustment layers. In a potential
    /// future where proper adjustment layers are added feel free to clean this up to be an animation with two adjustment
    /// layers rather than one mega layer.
    /// </summary>
    /// <param name="sprite">The spriteComponent we're adjusting the offset of</param>
    /// <param name="frequency">How many times per second does the animation run?</param>
    /// <param name="jitters">How many times should we jitter during the animation? Also determines breathing frequency</param>
    /// <param name="minJitter">Mininum jitter offset multiplier for X and Y directions</param>
    /// <param name="maxJitter">Maximum jitter offset multiplier for X and Y directions</param>
    /// <param name="breathing">Maximum breathing offset, this is in the Y direction</param>
    /// <param name="startOffset">Starting offset because we don't have adjustment layers</param>
    /// <param name="lastJitter">Last jitter so we don't jitter to the same quadrant</param>
    /// <returns></returns>
    public Animation GetFatigueAnimation(SpriteComponent sprite,
        float frequency,
        int jitters,
        Vector2 minJitter,
        Vector2 maxJitter,
        float breathing,
        Vector2 startOffset,
        ref Vector2 lastJitter)
    {
        // avoid animations with negative length or infinite length
        if (frequency <= 0)
            return new Animation();

        var breaths = new Vector2(0, breathing * 2) / jitters;

        var length =  1 / frequency;
        var frames = length / jitters;

        var keyFrames = new List<AnimationTrackProperty.KeyFrame> { new(sprite.Offset, 0f) };

        // Spits out a list of keyframes to feed to the AnimationPlayer based on the variables we've inputted
        for (var i = 1; i <= jitters; i++)
        {
            var offset = new Vector2(_random.NextFloat(minJitter.X, maxJitter.X),
                _random.NextFloat(minJitter.Y, maxJitter.Y));
            offset.X *= _random.Pick(_sign);
            offset.Y *= _random.Pick(_sign);

            if (i == 1 && Math.Sign(offset.X) == Math.Sign(lastJitter.X)
                       && Math.Sign(offset.Y) == Math.Sign(lastJitter.Y))
            {
                // If the sign is the same as last time on both axis we flip one randomly
                // to avoid jitter staying in one quadrant too much.
                if (_random.Prob(0.5f))
                    offset.X *= -1;
                else
                    offset.Y *= -1;
            }

            lastJitter = offset;

            // For the first half of the jitter, we vertically displace the sprite upwards to simulate breathing in
            if (i <= jitters / 2)
            {
                keyFrames.Add(new AnimationTrackProperty.KeyFrame(startOffset + breaths * i + offset, frames));
            }
            // For the next quarter we displace the sprite down, to about 12.5% breathing offset below our starting position
            // Simulates breathing out
            else if (i < jitters * 3 / 4)
            {
                keyFrames.Add(
                    new AnimationTrackProperty.KeyFrame(startOffset + breaths * ( jitters - i * 1.5f ) + offset, frames));
            }
            // Return to our starting position for breathing, jitter reaches its final position
            else
            {
                keyFrames.Add(
                    new AnimationTrackProperty.KeyFrame(startOffset + breaths * ( i - jitters ) + offset, frames));
            }
        }

        return new Animation
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    // Heavy Breathing
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Cubic,
                    KeyFrames = keyFrames,
                },
            }
        };
    }
}

public enum StunVisualLayers : byte
{
    StamCrit,
}
