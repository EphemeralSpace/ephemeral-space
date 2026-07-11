using Content.Server.Atmos.EntitySystems;
using Content.Shared._ES.Breakable;
using Content.Shared._ES.Breakable.Components;
using Content.Shared.Atmos.Components;

namespace Content.Server._ES.Breakable;

public sealed partial class ESBreakableWallSystem : ESSharedBreakableWallSystem
{
    [Dependency] private AirtightSystem _airtight = default!;

    protected override void OnBrokenStateChanged(Entity<ESBreakableWallComponent> ent, ref ESBrokenStateChanged args)
    {
        base.OnBrokenStateChanged(ent, ref args);

        if (TryComp<AirtightComponent>(ent, out var airtight))
            _airtight.SetAirblocked((ent.Owner, airtight), !args.Broken);
    }
}
