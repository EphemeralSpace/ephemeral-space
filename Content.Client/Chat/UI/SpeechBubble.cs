using System.Numerics;
using Content.Client._ES.Chat;
using Content.Client._ES.Core;
using Content.Client.Stylesheets;
using Content.Shared._ES.Chat;
using Content.Shared.CCVar;
using Content.Shared.Speech;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Chat.UI
{
    public abstract partial class SpeechBubble : Control
    {
        [Dependency] private IGameTiming _timing = default!;
        [Dependency] private IEyeManager _eyeManager = default!;
        [Dependency] private IEntityManager _entityManager = default!;
        [Dependency] protected IConfigurationManager ConfigManager = default!;
        protected readonly ESChatSystem Chat;
        private readonly SharedTransformSystem _transformSystem;

        /// <summary>
        ///     The total time a speech bubble stays on screen.
        /// </summary>
        private static readonly TimeSpan TotalTime = TimeSpan.FromSeconds(4);

        /// <summary>
        ///     The amount of time at the end of the bubble's life at which it starts fading.
        /// </summary>
        private static readonly TimeSpan FadeTime = TimeSpan.FromSeconds(0.25f);

        /// <summary>
        ///     The distance in world space to offset the speech bubble from the center of the entity.
        ///     i.e. greater -> higher above the mob's head.
        /// </summary>
        private const float EntityVerticalOffset = 0.5f;

        /// <summary>
        ///     The default maximum width for speech bubbles.
        /// </summary>
        public const float SpeechMaxWidth = 256;

        private readonly EntityUid _senderEntity;

        /// <summary>
        /// The time at which this bubble will die.
        /// </summary>
        private TimeSpan _deathTime;

        public float VerticalOffset { get; set; }
        private float _verticalOffsetAchieved;

        public Vector2 ContentSize { get; private set; }

        // man down
        public event Action<EntityUid, SpeechBubble>? OnDied;

        public static SpeechBubble CreateSpeechBubble(SpeechType type, ESChatMessage message, EntityUid senderEntity)
        {
            switch (type)
            {
                case SpeechType.Emote:
                    return new TextSpeechBubble(message, senderEntity, "emoteBox", prefix: "* ");

                case SpeechType.Say:
                    return new FancyTextSpeechBubble(message, senderEntity, "sayBox");

                case SpeechType.Whisper:
                    return new FancyTextSpeechBubble(message, senderEntity, "whisperBox");

                case SpeechType.Looc:
                    return new TextSpeechBubble(message, senderEntity, "emoteBox", Color.FromHex("#48d1cc"));

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public SpeechBubble(ESChatMessage message, EntityUid senderEntity, string speechStyleClass, Color? fontColor = null, string prefix = "")
        {
            IoCManager.InjectDependencies(this);
            _senderEntity = senderEntity;
            _transformSystem = _entityManager.System<SharedTransformSystem>();
            Chat = _entityManager.System<ESChatSystem>();

            // Use text clipping so new messages don't overlap old ones being pushed up.
            RectClipContent = true;

            var bubble = BuildBubble(message, speechStyleClass, fontColor, prefix);

            AddChild(bubble);

            ForceRunStyleUpdate();

            bubble.Measure(Vector2Helpers.Infinity);
            ContentSize = bubble.DesiredSize;
            _verticalOffsetAchieved = -ContentSize.Y;
            _deathTime = _timing.RealTime + TotalTime;
        }

        protected abstract Control BuildBubble(ESChatMessage message, string speechStyleClass, Color? fontColor = null, string? prefix = "");

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            var timeLeft = (float)(_deathTime - _timing.RealTime).TotalSeconds;
            if (_entityManager.Deleted(_senderEntity) || timeLeft <= 0)
            {
                // Timer spawn to prevent concurrent modification exception.
                Timer.Spawn(0, Die);
                return;
            }

            // Lerp to our new vertical offset if it's been modified.
            if (MathHelper.CloseToPercent(_verticalOffsetAchieved - VerticalOffset, 0, 0.1))
            {
                _verticalOffsetAchieved = VerticalOffset;
            }
            else
            {
                _verticalOffsetAchieved = MathHelper.Lerp(_verticalOffsetAchieved, VerticalOffset, 10 * args.DeltaSeconds);
            }

            if (!_entityManager.TryGetComponent<TransformComponent>(_senderEntity, out var xform) || xform.MapID != _eyeManager.CurrentEye.Position.MapId)
            {
                Modulate = Color.White.WithAlpha(0);
                return;
            }

            if (timeLeft <= FadeTime.TotalSeconds)
            {
                // Update alpha if we're fading.
                Modulate = Color.White.WithAlpha(timeLeft / (float)FadeTime.TotalSeconds);
            }
            else
            {
                // Make opaque otherwise, because it might have been hidden before
                Modulate = Color.White;
            }

            var baseOffset = 0f;

            if (_entityManager.TryGetComponent<SpeechComponent>(_senderEntity, out var speech))
                baseOffset = speech.SpeechBubbleOffset;

            var offset = (-_eyeManager.CurrentEye.Rotation).ToWorldVec() * -(EntityVerticalOffset + baseOffset);
            var worldPos = _transformSystem.GetWorldPosition(xform) + offset;

            var lowerCenter = (_eyeManager.WorldToScreen(worldPos) - (Parent?.GlobalPixelPosition ?? Vector2.Zero)) / UIScale;
            var screenPos = lowerCenter - new Vector2(ContentSize.X / 2, ContentSize.Y + _verticalOffsetAchieved);
            // Round to nearest 0.5
            screenPos = (screenPos * 2).Rounded() / 2;
            LayoutContainer.SetPosition(this, screenPos);

            var height = MathF.Ceiling(MathHelper.Clamp(lowerCenter.Y - screenPos.Y, 0, ContentSize.Y));
            SetHeight = height;
        }

        private void Die()
        {
            if (Disposed)
            {
                return;
            }

            OnDied?.Invoke(_senderEntity, this);
        }

        /// <summary>
        ///     Causes the speech bubble to start fading IMMEDIATELY.
        /// </summary>
        public void FadeNow()
        {
            if (_deathTime > _timing.RealTime)
            {
                _deathTime = _timing.RealTime + FadeTime;
            }
        }
    }

    public sealed class TextSpeechBubble : SpeechBubble
    {
        public TextSpeechBubble(ESChatMessage message, EntityUid senderEntity, string speechStyleClass, Color? fontColor = null, string prefix = "")
            : base(message, senderEntity, speechStyleClass, fontColor, prefix)
        {
        }

        protected override Control BuildBubble(ESChatMessage message, string speechStyleClass, Color? fontColor = null, string? prefix = "")
        {
            var label = new RichTextLabel
            {
                MaxWidth = SpeechMaxWidth,
                StyleClasses = { StyleClass.FontChat },
                ModulateSelfOverride = Color.White.WithAlpha(ConfigManager.GetCVar(CCVars.SpeechBubbleTextOpacity))
            };

            label.UnsafeSetMarkup($"{prefix}{message.Content}", fontColor);

            var panel = new PanelContainer
            {
                StyleClasses = { "speechBox", speechStyleClass },
                Children = { label },
            };

            return panel;
        }
    }

    public sealed class FancyTextSpeechBubble : SpeechBubble
    {

        public FancyTextSpeechBubble(ESChatMessage message, EntityUid senderEntity, string speechStyleClass, Color? fontColor = null, string prefix = "")
            : base(message, senderEntity, speechStyleClass, fontColor, prefix)
        {
        }

        protected override Control BuildBubble(ESChatMessage message, string speechStyleClass, Color? fontColor = null, string? prefix = "")
        {
            var bubbleContent = new RichTextLabel
            {
                MaxWidth = SpeechMaxWidth,
                Margin = new Thickness(2, 2, 2, 2),
                ModulateSelfOverride = Color.White.WithAlpha(ConfigManager.GetCVar(CCVars.SpeechBubbleTextOpacity)),
                StyleClasses = { "bubbleContent", StyleClass.FontChat },
            };

            // this is to remove color tags etc. so we just color it normal style
            var name = FormattedMessage.RemoveMarkupPermissive(message.Name);
            var content = FormattedMessage.RemoveMarkupPermissive(message.Content);
            var color = fontColor ?? Chat.GetChatColor(name);
            var outlineColor = Chat.GetChatOutlineColor(color);
            bubbleContent.OutlineColorOverride = outlineColor;
            bubbleContent.UnsafeSetMarkup($"{prefix}{content}", color);

            //As for below: Some day this could probably be converted to xaml. But that is not today. -Myr
            var mainPanel = new PanelContainer
            {
                StyleClasses = { "speechBox", speechStyleClass },
                Children = { bubbleContent },
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Bottom,
            };

            var panel = new PanelContainer
            {
                Children = { mainPanel }
            };

            return panel;
        }
    }
}
