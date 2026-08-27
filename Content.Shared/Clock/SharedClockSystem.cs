using System.Linq;
using Content.Shared._ES.Breakable;
using Content.Shared.Audio;
using Content.Shared.Examine;
using Content.Shared.GameTicking;

namespace Content.Shared.Clock;

public abstract partial class SharedClockSystem : EntitySystem
{
    [Dependency] private SharedGameTicker _ticker = default!;
    [Dependency] private SharedAmbientSoundSystem _ambientSound = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ClockComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ClockComponent, ESBrokenStateChanged>(OnBrokenStateChanged);
    }

    private void OnExamined(Entity<ClockComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("clock-examine", ("time", GetClockTimeText(ent))));
    }

    private void OnBrokenStateChanged(Entity<ClockComponent> ent, ref ESBrokenStateChanged args)
    {
        _ambientSound.SetAmbience(ent, !args.Broken);
        ent.Comp.StuckTime = args.Broken ? GetClockTime(ent) : null;
        Dirty(ent);
    }

    public string GetClockTimeText(Entity<ClockComponent> ent)
    {
        var time = GetClockTime(ent);
        switch (ent.Comp.ClockType)
        {
            case ClockType.TwelveHour:
                return time.ToString(@"h\:mm");
            case ClockType.TwentyFourHour:
                return time.ToString(@"hh\:mm");
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public TimeSpan GetGlobalTime()
    {
        return (EntityQuery<GlobalTimeManagerComponent>().FirstOrDefault()?.TimeOffset ?? TimeSpan.Zero) + _ticker.RoundDuration();
    }

    public void SetGlobalTime(TimeSpan time)
    {
        if (!TrySingle<GlobalTimeManagerComponent>(out var manager))
            return;

        manager.Value.Comp.TimeOffset = time - _ticker.RoundDuration();
        Dirty(manager.Value);
    }

    public TimeSpan GetClockTime(Entity<ClockComponent> ent)
    {
        var comp = ent.Comp;

        if (comp.StuckTime != null)
            return comp.StuckTime.Value;

        var time = GetGlobalTime();

        switch (comp.ClockType)
        {
            case ClockType.TwelveHour:
                var adjustedHours = time.Hours % 12;
                if (adjustedHours == 0)
                    adjustedHours = 12;
                return new TimeSpan(adjustedHours, time.Minutes, time.Seconds);
            case ClockType.TwentyFourHour:
                return time;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
