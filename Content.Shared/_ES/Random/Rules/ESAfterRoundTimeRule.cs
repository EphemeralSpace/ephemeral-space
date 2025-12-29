using Content.Shared.GameTicking;
using Content.Shared.Random.Rules;
using Robust.Shared.Timing;

namespace Content.Shared._ES.Random.Rules;

/// <summary>
///     Rule which returns true if the current time has passed the given time into the round.
/// </summary>
public sealed partial class ESAfterRoundTimeRule : RulesRule
{
    [DataField]
    public TimeSpan Time = TimeSpan.Zero;

    public override bool Check(EntityManager entManager, EntityUid uid)
    {
        // poop ass balls
        var ticker = entManager.EntitySysManager.GetEntitySystem<SharedGameTicker>();
        var curTime = IoCManager.Resolve<IGameTiming>().CurTime;

        if (curTime > ticker.RoundStartTimeSpan + Time)
            return !Inverted;

        return Inverted;
    }
}
