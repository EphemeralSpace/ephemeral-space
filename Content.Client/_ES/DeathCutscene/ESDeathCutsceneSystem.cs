using Content.Client._ES.Lobby;
using Content.Client.Audio;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.DeathCutscene;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._ES.DeathCutscene;

public sealed partial class ESDeathCutsceneSystem : EntitySystem
{
    [Dependency] private ContentAudioSystem _content = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private ESEntityTimerSystem _timer = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;
    private ESDiegeticLobbyUIController _curtains = default!;

    private static readonly TimeSpan CurtainCloseTime = TimeSpan.FromSeconds(9);
    private static readonly TimeSpan CurtainCloseDuration = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan CurtainOpenDuration = TimeSpan.FromSeconds(1.5);
    private static readonly SoundSpecifier PostDeathSound = new SoundPathSpecifier("/Audio/_ES/Ambience/death.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ESPlayDeathCutsceneNetworkEvent>(OnPlayDeathCutscene);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);

        _curtains = _ui.GetUIController<ESDiegeticLobbyUIController>();
    }

    private void OnPlayDeathCutscene(ESPlayDeathCutsceneNetworkEvent msg, EntitySessionEventArgs args)
    {
        _content.DisableAmbientMusic();
        _audio.PlayGlobal(PostDeathSound, Filter.Local(), false);
        _overlay.AddOverlay(new ESDeathCutsceneOverlay(_timing.RealTime));
        _timer.SpawnMethodTimer(CurtainCloseTime,
            () =>
            {
                _curtains.StartCurtainAnimation(false, CurtainCloseDuration);
            });
        _timer.SpawnMethodTimer(CurtainCloseTime + (CurtainCloseDuration * 2),
            () =>
            {
                _curtains.StartCurtainAnimation(true, CurtainOpenDuration);
            });
    }

    // stop the sequence on detach always
    private void OnPlayerDetached(LocalPlayerDetachedEvent ev)
    {
        if (_overlay.HasOverlay<ESDeathCutsceneOverlay>())
            _overlay.RemoveOverlay<ESDeathCutsceneOverlay>();
    }
}
