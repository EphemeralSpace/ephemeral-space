using Content.Client._ES.Lobby;
using Content.Shared._ES.Cinematic;
using Robust.Client.Audio;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._ES.Cinematic;

public sealed partial class ESCinematicUIController : UIController, IOnSystemChanged<ESClientCinematicSystem>
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ESDiegeticLobbyUIController _lobby = default!;
    [UISystemDependency] private readonly AudioSystem _audio = default!;

    private TimeSpan? _cinematicEndTime = null;
    private TimeSpan? _cinematicCloseCurtainTime = null;
    private ESCinematicPrototype? _currentCinematic = null;

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_cinematicEndTime is not { } time)
            return;

        if (_timing.RealTime > time)
        {
            _lobby.StartCurtainAnimation(true, _currentCinematic?.CurtainLength);

            _cinematicEndTime = null;
            _currentCinematic = null;

            if (UIManager.ActiveScreen?.GetWidget<CinematicContainer>() is { } container)
            {
                Log.Info("resetting texture");
                container.ResetCinematicTexture();
            }

        }
        else if (_cinematicCloseCurtainTime is { } closeTime && _timing.RealTime > closeTime)
        {
            _lobby.StartCurtainAnimation(false, _currentCinematic?.CurtainLength);
        }
    }

    public void OnSystemLoaded(ESClientCinematicSystem system)
    {
        system.CinematicRequested += OnCinematicRequested;
    }

    public void OnSystemUnloaded(ESClientCinematicSystem system)
    {
        system.CinematicRequested -= OnCinematicRequested;
    }

    private void OnCinematicRequested(ProtoId<ESCinematicPrototype> cinematic)
    {
        Log.Info("got cinematic request");

        if (UIManager.ActiveScreen?.GetWidget<CinematicContainer>() is not { } container
            || !_proto.TryIndex(cinematic, out var prototype))
            return;

        // dont play cinematic if we're already playing the same one
        if (_currentCinematic?.ID == cinematic.Id)
            return;

        Log.Info($"playing {cinematic.Id}");
        _currentCinematic = prototype;
        _cinematicEndTime = _timing.RealTime + prototype.Length;
        if (prototype.CurtainLength is { } curtainLength)
            _cinematicCloseCurtainTime = _cinematicEndTime - (curtainLength * 2);
        container.CinematicTexture.SetFromSpriteSpecifier(prototype.Animation);
        _audio.PlayGlobal(prototype.Sound, Filter.Local(), true);
    }
}
