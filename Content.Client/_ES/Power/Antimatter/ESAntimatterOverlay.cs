using System.Numerics;
using Content.Client.Graphics;
using Content.Client.Light;
using Content.Shared._ES.Power.Antimatter.Components;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client._ES.Power.Antimatter;

public sealed class ESAntimatterOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";

    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly HashSet<Entity<ESAntimatterComponent>> _antimatterSet = new();

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly OverlayResourceCache<CachedResources> _resources = new();

    public ESAntimatterOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = AfterLightTargetOverlay.ContentZIndex + 1;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var viewport = args.Viewport;
        var mapId = args.MapId;
        var worldBounds = args.WorldBounds;
        var worldHandle = args.WorldHandle;
        var target = viewport.RenderTarget;
        var lightScale = target.Size / (Vector2) viewport.Size;
        var scale = viewport.RenderScale / (Vector2.One / lightScale);
        var lookups = _entManager.System<EntityLookupSystem>();
        var xformSystem = _entManager.System<SharedTransformSystem>();

        var reducedMotion = _cfgManager.GetCVar(CCVars.ReducedMotion);

        var res = _resources.GetForViewport(viewport, static _ => new CachedResources());

        if (res.RenderTarget?.Texture.Size != target.Size)
        {
            res.RenderTarget?.Dispose();
            res.RenderTarget = _clyde.CreateRenderTarget(target.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "ambient-occlusion-target");
        }

        if (res.BlurBuffer?.Texture.Size != target.Size)
        {
            res.BlurBuffer?.Dispose();
            res.BlurBuffer = _clyde.CreateRenderTarget(target.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "ambient-occlusion-blur-target");
        }

        // Draw the texture data to the texture.
        args.WorldHandle.RenderInRenderTarget(res.RenderTarget,
            () =>
            {
                worldHandle.UseShader(_proto.Index(UnshadedShader).Instance());
                var invMatrix = res.RenderTarget.GetWorldToLocalMatrix(viewport.Eye!, scale);

                _antimatterSet.Clear();
                lookups.GetEntitiesIntersecting(mapId, worldBounds.Enlarged(2), _antimatterSet);
                foreach (var entry in _antimatterSet)
                {
                    var matrix = xformSystem.GetWorldMatrix(entry);
                    var localMatrix = Matrix3x2.Multiply(matrix, invMatrix);

                    worldHandle.SetTransform(localMatrix);

                    var width = (!reducedMotion
                        ? new Vector2(_random.NextFloat(12f, 13f), _random.NextFloat(12f, 13f))
                        : Vector2.One * 12f)
                                / EyeManager.PixelsPerMeter;

                    var box = Box2.UnitCentered with
                    {
                        BottomLeft = Box2.UnitCentered.BottomLeft - width,
                        TopRight = Box2.UnitCentered.TopRight + width,
                    };
                    worldHandle.DrawRect(box, Color.White);
                }
            },
            Color.Transparent);

        var offset = !reducedMotion
            ? _random.NextFloat(-2, 2)
            : 0;
        _clyde.BlurRenderTarget(viewport, res.RenderTarget, res.BlurBuffer, viewport.Eye!, 14f * 2 + offset);

        worldHandle.DrawTextureRect(res.RenderTarget!.Texture, worldBounds, Color.Black);

        args.WorldHandle.SetTransform(Matrix3x2.Identity);
        args.WorldHandle.UseShader(null);
    }

    private sealed class CachedResources : IDisposable
    {
        public IRenderTexture? RenderTarget = null;
        public IRenderTexture? BlurBuffer = null;

        public void Dispose()
        {
            RenderTarget?.Dispose();
            BlurBuffer?.Dispose();
        }
    }
}
