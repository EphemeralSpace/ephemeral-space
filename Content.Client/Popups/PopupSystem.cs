using System.Linq;
using System.Numerics;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Replays;
using Robust.Shared.Timing;

namespace Content.Client.Popups
{
    public sealed partial class PopupSystem : SharedPopupSystem
    {
        [Dependency] private IConfigurationManager _configManager = default!;
        [Dependency] private IInputManager _inputManager = default!;
        [Dependency] private IOverlayManager _overlay = default!;
        [Dependency] private IPlayerManager _playerManager = default!;
        [Dependency] private IPrototypeManager _prototype = default!;
        [Dependency] private IGameTiming _timing = default!;
        [Dependency] private IRobustRandom _random = default!;
        [Dependency] private IUserInterfaceManager _uiManager = default!;
        [Dependency] private IReplayRecordingManager _replayRecording = default!;
        [Dependency] private ExamineSystemShared _examine = default!;
        [Dependency] private SharedTransformSystem _transform = default!;

        public IReadOnlyCollection<WorldPopupLabel> WorldLabels => _aliveWorldLabels.Values;
        public IReadOnlyCollection<CursorPopupLabel> CursorLabels => _aliveCursorLabels.Values;

        private readonly Dictionary<WorldPopupData, WorldPopupLabel> _aliveWorldLabels = new();
        private readonly Dictionary<CursorPopupData, CursorPopupLabel> _aliveCursorLabels = new();

        private readonly List<IPopupPredictionInstance> _predictionInstances = new();

        public const float MaximumPopupRandomOffsetMagnitude = 20f;
        public const float MinimumPopupLifetime = 1f;
        public const float MaximumPopupLifetime = 4f;
        public const float PopupLifetimePerCharacter = 0.1f;

        public override void Initialize()
        {
            SubscribeNetworkEvent<PopupCursorEvent>(OnPopupCursorEvent);
            SubscribeNetworkEvent<PopupCoordinatesEvent>(OnPopupCoordinatesEvent);
            SubscribeNetworkEvent<PopupEntityEvent>(OnPopupEntityEvent);
            SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestart);
            _overlay
                .AddOverlay(new PopupOverlay(
                    _configManager,
                    EntityManager,
                    _playerManager,
                    _prototype,
                    _uiManager,
                    _uiManager.GetUIController<PopupUIController>(),
                    _examine,
                    _transform,
                    this));
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _overlay
                .RemoveOverlay<PopupOverlay>();
        }

        private void WrapAndRepeatPopup(PopupLabel existingLabel, string popupMessage)
        {
            existingLabel.TotalTime = 0;
            existingLabel.Repeats += 1;
            existingLabel.Text = Loc.GetString("popup-system-repeated-popup-stacking-wrap",
                ("popup-message", popupMessage),
                ("count", existingLabel.Repeats));
        }

        private void PopupMessage(string? message, PopupType type, EntityCoordinates coordinates, EntityUid? entity, bool recordReplay)
        {
            if (message == null)
                return;

            if (recordReplay && _replayRecording.IsRecording)
            {
                if (entity != null)
                    _replayRecording.RecordClientMessage(new PopupEntityEvent(message, type, Timing.CurTick, GetNetEntity(entity.Value)));
                else
                    _replayRecording.RecordClientMessage(new PopupCoordinatesEvent(message, type, Timing.CurTick, GetNetCoordinates(coordinates)));
            }

            var popupData = new WorldPopupData(message, type, coordinates, entity);
            if (_aliveWorldLabels.TryGetValue(popupData, out var existingLabel))
            {
                WrapAndRepeatPopup(existingLabel, popupData.Message);
                return;
            }

            var label = new WorldPopupLabel(coordinates)
            {
                Text = message,
                Type = type,
                RandomOffset = _random.NextVector2(MaximumPopupRandomOffsetMagnitude)
            };

            _aliveWorldLabels.Add(popupData, label);
        }

        #region Abstract Method Implementations
        public override void PopupCoordinates(string? message, EntityCoordinates coordinates, PopupType type = PopupType.Small)
        {
            if (!Timing.IsFirstTimePredicted || message is null)
                return;

            _predictionInstances.Add(new PopupCoordinatesEvent.PredictionInstance(message, type, Timing.CurTick, GetNetCoordinates(coordinates)));
            PopupMessage(message, type, coordinates, null, true);
        }

        public override void PopupCoordinates(string? message, EntityCoordinates coordinates, ICommonSession recipient, PopupType type = PopupType.Small)
        {
            if (_playerManager.LocalSession == recipient)
                PopupMessage(message, type, coordinates, null, true);
        }

        public override void PopupCoordinates(string? message, EntityCoordinates coordinates, EntityUid recipient, PopupType type = PopupType.Small)
        {
            if (_playerManager.LocalEntity == recipient)
                PopupMessage(message, type, coordinates, null, true);
        }

        private void PopupCursorInternal(string? message, PopupType type, bool recordReplay)
        {
            if (message == null)
                return;

            if (recordReplay && _replayRecording.IsRecording)
                _replayRecording.RecordClientMessage(new PopupCursorEvent(message, type, Timing.CurTick));

            var popupData = new CursorPopupData(message, type);
            if (_aliveCursorLabels.TryGetValue(popupData, out var existingLabel))
            {
                WrapAndRepeatPopup(existingLabel, popupData.Message);
                return;
            }

            var label = new CursorPopupLabel(_inputManager.MouseScreenPosition)
            {
                Text = message,
                Type = type,
                RandomOffset = _random.NextVector2(MaximumPopupRandomOffsetMagnitude)
            };

            _aliveCursorLabels.Add(popupData, label);
        }

        /// <summary>
        ///     Shows a popup at the local users' cursor. Does nothing on the server.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="type">Used to customize how this popup should appear visually.</param>
        public void PopupCursor(string? message, PopupType type = PopupType.Small)
        {
            if (!_timing.IsFirstTimePredicted || message is null)
                return;

            _predictionInstances.Add(new PopupCursorEvent.PredictionInstance(message, type, Timing.CurTick));
            PopupCursorInternal(message, type, true);
        }

        public override void PopupCursor(string? message, ICommonSession recipient, PopupType type = PopupType.Small)
        {
            if (_playerManager.LocalSession == recipient)
                PopupCursor(message, type);
        }

        public override void PopupCursor(string? message, EntityUid? recipient, PopupType type = PopupType.Small)
        {
            if (_playerManager.LocalEntity == recipient)
                PopupCursor(message, type);
        }

        public override void PopupCoordinates(string? message, EntityCoordinates coordinates, Filter filter, bool replayRecord, PopupType type = PopupType.Small)
        {
            PopupCoordinates(message, coordinates, type);
        }

        public override void PopupEntity(string? message, EntityUid uid, EntityUid? recipient, PopupType type = PopupType.Small)
        {
            if (_playerManager.LocalEntity == recipient)
                PopupEntity(message, uid, type);
        }

        public override void PopupEntity(string? message, EntityUid uid, ICommonSession recipient, PopupType type = PopupType.Small)
        {
            if (_playerManager.LocalSession == recipient)
                PopupEntity(message, uid, type);
        }

        public override void PopupEntity(string? message, EntityUid uid, Filter filter, bool recordReplay, PopupType type = PopupType.Small)
        {
            if (!filter.Recipients.Contains(_playerManager.LocalSession))
                return;

            PopupEntity(message, uid, type);
        }

        public override void PopupEntity(string? message, EntityUid uid, PopupType type = PopupType.Small)
        {
            if (message is null || !Timing.IsFirstTimePredicted)
                return;

            if (!TryComp(uid, out TransformComponent? transform))
                return;

            _predictionInstances.Add(new PopupEntityEvent.PredictionInstance(message, type, Timing.CurTick, GetNetEntity(uid)));
            PopupMessage(message, type, transform.Coordinates, uid, true);
        }

        #endregion

        #region Network Event Handlers

        private void OnPopupCursorEvent(PopupCursorEvent ev)
        {
            var instance = new PopupCursorEvent.PredictionInstance(ev.Message, ev.Type, ev.Tick);
            if (_predictionInstances.Remove(instance))
                return;

            PopupCursorInternal(ev.Message, ev.Type, false);
        }

        private void OnPopupCoordinatesEvent(PopupCoordinatesEvent ev)
        {
            var instance = new PopupCoordinatesEvent.PredictionInstance(ev.Message, ev.Type, ev.Tick, ev.Coordinates);
            if (_predictionInstances.Remove(instance))
                return;

            PopupMessage(ev.Message, ev.Type, GetCoordinates(ev.Coordinates), null, false);
        }

        private void OnPopupEntityEvent(PopupEntityEvent ev)
        {
            var instance = new PopupEntityEvent.PredictionInstance(ev.Message, ev.Type, ev.Tick, ev.Uid);
            if (_predictionInstances.Remove(instance))
                return;

            var entity = GetEntity(ev.Uid);

            if (TryComp(entity, out TransformComponent? transform))
                PopupMessage(ev.Message, ev.Type, transform.Coordinates, entity, false);
        }

        private void OnRoundRestart(RoundRestartCleanupEvent ev)
        {
            _aliveCursorLabels.Clear();
            _aliveWorldLabels.Clear();
        }

        #endregion

        public static float GetPopupLifetime(PopupLabel label)
        {
            return Math.Clamp(PopupLifetimePerCharacter * label.Text.Length,
                MinimumPopupLifetime,
                MaximumPopupLifetime);
        }

        public override void FrameUpdate(float frameTime)
        {
            if (_predictionInstances.Count != 0)
            {
                _predictionInstances.RemoveAll(p => (int) Timing.CurTick.Value - (int) p.Tick.Value > 5000);
            }

            if (_aliveWorldLabels.Count == 0 && _aliveCursorLabels.Count == 0)
                return;

            if (_aliveWorldLabels.Count > 0)
            {
                var aliveWorldToRemove = new ValueList<WorldPopupData>();
                foreach (var (data, label) in _aliveWorldLabels)
                {
                    label.TotalTime += frameTime;
                    if (label.TotalTime > GetPopupLifetime(label) || Deleted(label.InitialPos.EntityId))
                    {
                        aliveWorldToRemove.Add(data);
                    }
                }
                foreach (var data in aliveWorldToRemove)
                {
                    _aliveWorldLabels.Remove(data);
                }
            }

            if (_aliveCursorLabels.Count > 0)
            {
                var aliveCursorToRemove = new ValueList<CursorPopupData>();
                foreach (var (data, label) in _aliveCursorLabels)
                {
                    label.TotalTime += frameTime;
                    if (label.TotalTime > GetPopupLifetime(label))
                    {
                        aliveCursorToRemove.Add(data);
                    }
                }
                foreach (var data in aliveCursorToRemove)
                {
                    _aliveCursorLabels.Remove(data);
                }
            }
        }

        public abstract class PopupLabel
        {
            public PopupType Type = PopupType.Small;
            public string Text { get; set; } = string.Empty;
            public float TotalTime { get; set; }
            public Vector2 RandomOffset = Vector2.Zero;
            public int Repeats = 1;
        }

        public sealed class WorldPopupLabel(EntityCoordinates coordinates) : PopupLabel
        {
            /// <summary>
            /// The original EntityCoordinates of the label.
            /// </summary>
            public EntityCoordinates InitialPos = coordinates;
        }

        public sealed class CursorPopupLabel(ScreenCoordinates screenCoords) : PopupLabel
        {
            public ScreenCoordinates InitialPos = screenCoords;
        }

        [UsedImplicitly]
        private record struct WorldPopupData(
            string Message,
            PopupType Type,
            EntityCoordinates Coordinates,
            EntityUid? Entity);

        [UsedImplicitly]
        private record struct CursorPopupData(
            string Message,
            PopupType Type);
    }
}
