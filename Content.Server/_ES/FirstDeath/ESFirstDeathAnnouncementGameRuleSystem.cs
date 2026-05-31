using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Shared._ES.KillTracking.Components;
using Robust.Server.Audio;
using Robust.Shared.Player;

namespace Content.Server._ES.FirstDeath;

/// <summary>
///     Handles playing a sound when the first player in a round dies.
/// </summary>
public sealed partial class ESFirstDeathAnnouncementGameRuleSystem : GameRuleSystem<ESFirstDeathAnnouncementGameRuleComponent>
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private MindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESPlayerKilledEvent>(OnPlayerKilled);
    }

    private void OnPlayerKilled(ref ESPlayerKilledEvent ev)
    {
        // don't play for mindless simplemobs
        if (!_mind.TryGetMind(ev.Killed, out _))
            return;

        // this intentionally triggers on non-player kills also (suicides environment etc)
        var query = EntityQueryEnumerator<ESFirstDeathAnnouncementGameRuleComponent>();
        while (query.MoveNext(out _, out var announcement))
        {
            if (announcement.PlayedSound)
                continue;

            _audio.PlayGlobal(announcement.Sound, Filter.Broadcast(), recordReplay: true, announcement.Sound.Params.WithVolume(-7f));
            announcement.PlayedSound = true;
        }
    }
}
