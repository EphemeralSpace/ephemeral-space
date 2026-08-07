using Content.Client.GameTicking.Managers;
using Content.Shared._DV.Screens;
using Robust.Shared.Timing;

namespace Content.Client._DV.Screens;

public sealed partial class DVScreenSystem : DVSharedScreenSystem
{
    [Dependency] private DVTextVisualsSystem _textVisuals = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ClientGameTicker _ticker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DVScreenComponent, AfterAutoHandleStateEvent>(OnScreenState);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<DVScreenComponent>();
        while (query.MoveNext(out var uid, out var screen))
        {
            switch (screen.Content)
            {
                case DVScreenContent.Text:
                    break;
                case DVScreenContent.CurrentTime:
                    CurrentTime((uid, screen));
                    break;
                case DVScreenContent.GenericTargetTime:
                    GenericTargetTime((uid, screen));
                    break;
                case DVScreenContent.EstimatedTimeOfArrival:
                    EstimatedTimeOfArrival((uid, screen));
                    break;
                case DVScreenContent.AlertLevel:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void OnScreenState(Entity<DVScreenComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(ent);

        switch (ent.Comp.Content)
        {
            case DVScreenContent.Text:
                Text(ent);
                break;
            case DVScreenContent.CurrentTime:
                CurrentTime(ent);
                break;
            case DVScreenContent.GenericTargetTime:
                GenericTargetTime(ent);
                break;
            case DVScreenContent.EstimatedTimeOfArrival:
                EstimatedTimeOfArrival(ent);
                break;
            case DVScreenContent.AlertLevel:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void Text(Entity<DVScreenComponent> ent)
    {
        _textVisuals.SetText(ent.Owner, ent.Comp.Line1, ent.Comp.Line2);
    }

    private void CurrentTime(Entity<DVScreenComponent> ent)
    {
        var time = (_timing.CurTime - _ticker.RoundStartTimeSpan).Duration();
        _textVisuals.SetText(ent.Owner, Loc.GetString("status-display-time"), time.ToString("hh\\:mm"));
    }

    private void GenericTargetTime(Entity<DVScreenComponent> ent)
    {
        if (ent.Comp.TargetTime <= _timing.CurTime)
        {
            _textVisuals.SetText(ent.Owner, string.Empty, string.Empty);
            return;
        }

        var time = (_timing.CurTime - ent.Comp.TargetTime).Duration();
        var formatted = $"{(int)time.TotalMinutes}:{time.Seconds:D2}";
        var title = ent.Comp.Line1;

        _textVisuals.SetText(ent.Owner, title, formatted);
    }

    private void EstimatedTimeOfArrival(Entity<DVScreenComponent> ent)
    {
        if (ent.Comp.TargetTime <= _timing.CurTime)
        {
            _textVisuals.SetText(ent.Owner, string.Empty, string.Empty);
            return;
        }

        var time = (_timing.CurTime - ent.Comp.TargetTime).Duration();
        var formatted = time.ToString("mm\\:ss");
        var title = ent.Comp.ScreenIsAtDestination ? Loc.GetString("status-display-etd") : Loc.GetString("status-display-eta");

        _textVisuals.SetText(ent.Owner, title, formatted);
    }
}
