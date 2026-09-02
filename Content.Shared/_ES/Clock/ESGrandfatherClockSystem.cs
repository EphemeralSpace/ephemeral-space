using Content.Shared._ES.Breakable;
using Content.Shared._ES.Clock.Components;
using Content.Shared._ES.Core.Timer;
using Content.Shared.Clock;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._ES.Clock;

public sealed partial class ESGrandfatherClockSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ESBreakableSystem _breakable = default!;
    [Dependency] private SharedClockSystem _clock = default!;
    [Dependency] private ESEntityTimerSystem _entityTimer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESGrandfatherClockComponent, ESGrandfatherClockChimeTimerEvent>(OnClockChime);
    }

    private void OnClockChime(Entity<ESGrandfatherClockComponent> ent, ref ESGrandfatherClockChimeTimerEvent args)
    {
        // Audio prediction
        if (_net.IsClient)
            return;

        if (_breakable.IsBroken(ent.Owner))
            return;

        _audio.PlayPvs(ent.Comp.ChimeSound, ent);
    }

    public override void Update(float frameTime)
    {
        foreach (var (uid, comp, clock) in EntityQueryEnumerator<ESGrandfatherClockComponent, ClockComponent>())
        {
            var time = _clock.GetClockTime((uid, clock));
            var hour = time.Hours;

            // only start chiming on the hour
            if (comp.LastHour == hour ||
                time.Minutes != 0)
                continue;

            comp.LastHour = hour;
            Chime((uid, comp), time);
        }
    }

    public void Chime(Entity<ESGrandfatherClockComponent> ent, TimeSpan time)
    {
        if (_net.IsClient)
            return;
        _audio.PlayPvs(ent.Comp.MelodySound, ent);

        for (var i = 0; i < time.Hours; ++i)
        {
            // I don't care about this serializing.
            _entityTimer.SpawnTimer(ent, ent.Comp.MelodyDelay + ent.Comp.ChimeDelay * i, new ESGrandfatherClockChimeTimerEvent());
        }
    }
}
