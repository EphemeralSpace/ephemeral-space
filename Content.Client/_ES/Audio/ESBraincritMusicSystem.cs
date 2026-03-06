using Content.Client.Audio;
using Content.Shared._Offbrand.Wounds;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Client.Audio;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Client._ES.Audio;

/// <summary>
///     Handles playing/starting/stopping the braincrit music correctly when you enter it
/// </summary>
public sealed class ESBraincritMusicSystem : EntitySystem
{
    [Dependency] private readonly ContentAudioSystem _content = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private static ResPath _braincritMusicPath = new("/Audio/_ES/Ambience/approach.ogg");
    private Entity<AudioComponent>? _audioStream = null;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayAmbientMusicEvent>(OnPlayAmbientMusic);
        SubscribeLocalEvent<BrainDamageComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttach);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetach);
    }

    private void OnPlayAmbientMusic(ref PlayAmbientMusicEvent ev)
    {
        if (_player.LocalEntity is { } entity && TryComp<MobStateComponent>(entity, out var state) && state.CurrentState == MobState.Critical)
            ev.Cancelled = true;
    }

    private void OnMobStateChanged(Entity<BrainDamageComponent> ent, ref MobStateChangedEvent args)
    {
        UpdateMusicState(ent, args.NewMobState);
    }

    private void OnPlayerAttach(LocalPlayerAttachedEvent ev)
    {
        if (!TryComp<MobStateComponent>(ev.Entity, out var mobState))
            return;

        UpdateMusicState(ev.Entity, mobState.CurrentState);
    }

    private void OnPlayerDetach(LocalPlayerDetachedEvent ev)
    {
        if (_audio.IsPlaying(_audioStream))
            _content.FadeOut(_audioStream, duration: 2f);
    }

    private void UpdateMusicState(EntityUid entity, MobState mobState)
    {
        if (entity != _player.LocalEntity)
            return;

        // entering mobstate crit is handled by brain stuff when damage/oxygen are too low
        // so this corresponds with 'braincrit' as im describing it more or less
        // (as opposed to paincrit which is not actually mobstate critical)
        // also we dont null it out
        if (mobState != MobState.Critical)
        {
            if (_audio.IsPlaying(_audioStream))
                _content.FadeOut(_audioStream, duration: 2f);
        }
        else
        {
            var audio = new SoundPathSpecifier(_braincritMusicPath);
            _audioStream = _audio.PlayGlobal(audio, Filter.Local(), false);
            // force ambient music to stop if it hasnt already
            _content.UpdateAmbientMusic();
        }
    }
}
