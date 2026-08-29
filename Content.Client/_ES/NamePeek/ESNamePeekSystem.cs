using Content.Client._ES.Chat;
using Content.Client.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Input;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Light;

namespace Content.Client._ES.NamePeek;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class ESNamePeekSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private ExamineSystem _examine = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private LightLevelSystem _lightLevel = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private ESChatSystem _chat = default!;

    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery;
    [Dependency] private EntityQuery<TransformComponent> _transformQuery;
    [Dependency] private EntityQuery<MobStateComponent> _mobstateQuery;
    [Dependency] private EntityQuery<HumanoidAppearanceComponent> _humanoidAppearanceQuery;

    public bool Visible;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;

        _overlay.AddOverlay(new NamePeekOverlay(
            _examine,
            _lookup,
            this,
            _chat,
            _lightLevel,
            _transform,
            _sprite,
            _spriteQuery,
            _transformQuery,
            _mobstateQuery,
            _humanoidAppearanceQuery));

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ESHoldToFace, new PointerInputCmdHandler(OnExamineNames, ignoreUp: false, outsidePrediction: true))
            .Register<ESNamePeekSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<NamePeekOverlay>();
    }

    private bool OnExamineNames(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (_player.LocalEntity == null)
            return false;

        switch (args.State)
        {
            case BoundKeyState.Down:
                Visible = true;
                break;
            case BoundKeyState.Up:
                Visible = false;
                break;
        }

        //Return false so it doesn't take priority over other stuff
        return false;
    }
}
