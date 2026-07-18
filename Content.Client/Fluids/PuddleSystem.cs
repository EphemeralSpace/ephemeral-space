using Content.Shared._Citadel.Utilities;
using Content.Shared.Chemistry.Components;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Client.Fluids;

public sealed partial class PuddleSystem : SharedPuddleSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PuddleComponent, AppearanceChangeEvent>(OnPuddleAppearance);
    }

    private void OnPuddleAppearance(EntityUid uid, PuddleComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var volume = 1f;

        if (args.AppearanceData.TryGetValue(PuddleVisuals.CurrentVolume, out var volumeObj))
        {
            volume = (float)volumeObj;
        }

        var spriteSetPrototype = DefaultPuddleSpriteSet;
        if (args.AppearanceData.TryGetValue(PuddleVisuals.SpriteSet, out var spriteSetObj))
        {
            spriteSetPrototype = (string)spriteSetObj;
        }

        var spriteSet = ProtoMan.Index(spriteSetPrototype);

        var sprites = volume switch
        {
            < LowThreshold => spriteSet.SmallSprites,
            < MediumThreshold => spriteSet.MediumSprites,
            _ => spriteSet.LargeSprites,
        };

        IRobustRandom random = new RngSeed().SeedForStep(GetNetEntity(uid).Id + sprites.GetHashCode()).IntoRandomizer();
        _sprite.LayerSetSprite((uid, args.Sprite), 0, random.Pick(sprites));
        _sprite.LayerSetRotation((uid, args.Sprite), 0, random.NextAngle().RoundToCardinalAngle());

        var baseColor = spriteSet.BaseColor;

        if (spriteSet.Recolor && args.AppearanceData.TryGetValue(PuddleVisuals.SolutionColor, out var colorObj))
        {
            var color = (Color)colorObj;
            _sprite.SetColor((uid, args.Sprite), color * baseColor);
        }
        else
        {
            _sprite.SetColor((uid, args.Sprite), baseColor);
        }
    }

    #region Spill

    // Maybe someday we'll have clientside prediction for entity spawning, but not today.
    // Until then, these methods do nothing on the client.
    /// <inheritdoc/>
    public override bool TrySplashSpillAt(Entity<SpillableComponent?> entity, EntityCoordinates coordinates, out EntityUid puddleUid, out Solution solution, bool sound = true, EntityUid? user = null)
    {
        puddleUid = EntityUid.Invalid;
        solution = new Solution();
        return false;
    }

    public override bool TrySplashSpillAt(EntityUid entity,
        EntityCoordinates coordinates,
        Solution spilled,
        out EntityUid puddleUid,
        bool sound = true,
        EntityUid? user = null)
    {
        puddleUid = EntityUid.Invalid;
        return false;
    }

    /// <inheritdoc/>
    public override bool TrySpillAt(EntityCoordinates coordinates, Solution solution, out EntityUid puddleUid, bool sound = true)
    {
        puddleUid = EntityUid.Invalid;
        return false;
    }

    /// <inheritdoc/>
    public override bool TrySpillAt(EntityUid uid, Solution solution, out EntityUid puddleUid, bool sound = true, TransformComponent? transformComponent = null)
    {
        puddleUid = EntityUid.Invalid;
        return false;
    }

    /// <inheritdoc/>
    public override bool TrySpillAt(TileRef tileRef, Solution solution, out EntityUid puddleUid, bool sound = true, bool tileReact = true)
    {
        puddleUid = EntityUid.Invalid;
        return false;
    }

    #endregion Spill
}
