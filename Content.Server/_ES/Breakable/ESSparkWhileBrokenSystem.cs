using Content.Server._ES.Breakable.Components;
using Content.Shared._ES.Breakable;
using Content.Shared._ES.Sparks;
using Robust.Shared.Random;

namespace Content.Server._ES.Breakable;

public sealed partial class ESSparkWhileBrokenSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ESBreakableSystem _breakable = default!;
    [Dependency] private ESSparksSystem _sparks = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var ent in EntityQueryEnumerator<ESSparkWhileBrokenComponent>())
        {
            if (!_random.Prob(ent.Comp.SparkChancePerSecond * frameTime))
                continue;

            if (!_breakable.IsBroken(ent.Owner))
                continue;

            _sparks.DoSparks(ent);
        }
    }
}
