using Content.Server._ES.Power.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._ES.Breakable;

namespace Content.Server._ES.Power;

public sealed partial class ESBreakableBatterySystem : EntitySystem
{
    [Dependency] private BatterySystem _battery = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESBreakableBatteryComponent, ESBrokenStateChanged>(OnBrokenStateChanged);
    }

    private void OnBrokenStateChanged(Entity<ESBreakableBatteryComponent> ent, ref ESBrokenStateChanged args)
    {
        // Empty battery on break
        if (args.Broken)
            _battery.SetCharge(ent.Owner, 0f);

        if (TryComp<PowerNetworkBatteryComponent>(ent, out var battery))
            battery.Enabled = !args.Broken;
    }
}
