using Content.Server.GameTicking;
using Content.Shared._ES.Chat;
using Content.Shared._ES.Tips;
using Content.Shared.CCVar;
using Content.Shared.Tips;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Tips;

public sealed partial class TipsSystem : SharedTipsSystem
{
    [Dependency] private IESSharedChatManager _chat = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private ESTipsManager _tips = default!;

    private bool _tipsEnabled;
    private float _tipTimeOutOfRound;
    private float _tipTimeInRound;
    private float _tipTippyChance;

    [ViewVariables(VVAccess.ReadWrite)]
    private TimeSpan _nextTipTime = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnGameRunLevelChanged);
        Subs.CVar(_cfg, CCVars.TipsEnabled, SetEnabled, true);
        Subs.CVar(_cfg, CCVars.TipFrequencyOutOfRound, value => _tipTimeOutOfRound = value, true);
        Subs.CVar(_cfg, CCVars.TipFrequencyInRound, value => _tipTimeInRound = value, true);
        Subs.CVar(_cfg, CCVars.TipsTippyChance, value => _tipTippyChance = value, true);

        RecalculateNextTipTime();
    }

    private void OnGameRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        // reset for lobby -> inround
        // reset for inround -> post but not post -> lobby
        if (ev.New == GameRunLevel.InRound || ev.Old == GameRunLevel.InRound)
        {
            RecalculateNextTipTime();
        }
    }

    private void SetEnabled(bool value)
    {
        _tipsEnabled = value;

        if (_nextTipTime != TimeSpan.Zero)
            RecalculateNextTipTime();
    }

    public override void RecalculateNextTipTime()
    {
        if (_ticker.RunLevel == GameRunLevel.InRound)
        {
            _nextTipTime = _timing.CurTime + TimeSpan.FromSeconds(_tipTimeInRound);
        }
        else
        {
            _nextTipTime = _timing.CurTime + TimeSpan.FromSeconds(_tipTimeOutOfRound);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_tipsEnabled)
            return;

        if (_nextTipTime != TimeSpan.Zero && _timing.CurTime > _nextTipTime)
        {
            AnnounceRandomTip();
            RecalculateNextTipTime();
        }
    }

    public override void SendTippy(
        string message,
        EntProtoId? prototype = null,
        float speakTime = 5f,
        float slideTime = 3f,
        float waddleInterval = 0.5f)
    {
        var ev = new TippyEvent(message, prototype, speakTime, slideTime, waddleInterval);
        RaiseNetworkEvent(ev);
    }

    public override void SendTippy(
        ICommonSession session,
        string message,
        EntProtoId? prototype = null,
        float speakTime = 5f,
        float slideTime = 3f,
        float waddleInterval = 0.5f)
    {
        var ev = new TippyEvent(message, prototype, speakTime, slideTime, waddleInterval);
        RaiseNetworkEvent(ev, session);
    }

    public override void AnnounceRandomTip()
    {
        var tip = _tips.GetRandomTip();
        var msg = Loc.GetString("tips-system-chat-message-wrap", ("tip", Loc.GetString(tip)));

        if (_random.Prob(_tipTippyChance))
        {
            var speakTime = GetSpeechTime(msg);
            SendTippy(msg, speakTime: speakTime);
        }
        else
        {
            _chat.SendChatMessage(
                msg,
                IESSharedChatManager.ServerChannel,
                null,
                recordReplay: false,
                color: Color.MediumPurple);
        }
    }
}
