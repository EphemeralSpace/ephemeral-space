using Content.Server._ES.Breakable.Components;
using Content.Shared._ES.Breakable;
using Content.Shared._ES.Sparks;
using Robust.Shared.Random;

namespace Content.Server._ES.Breakable;

public sealed partial class ESSparkWhileBrokenSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ESSparksSystem _sparks = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESSparkWhileBrokenComponent, ESBrokenStateChanged>(OnBrokenStateChanged);
    }

    private void OnBrokenStateChanged(Entity<ESSparkWhileBrokenComponent> ent, ref ESBrokenStateChanged args)
    {
        ent.Comp.Enabled = args.Broken;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var ent in EntityQueryEnumerator<ESSparkWhileBrokenComponent>())
        {
            if (!ent.Comp.Enabled)
                continue;

            if (!_random.Prob(ent.Comp.SparkChancePerSecond * frameTime))
                continue;

            _sparks.DoSparks(ent);
        }
    }
}
