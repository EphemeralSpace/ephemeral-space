using Content.Server._ES.Breakable.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Construction;
using Content.Shared._ES.Breakable;
using Content.Shared._ES.Breakable.Components;
using Content.Shared.Atmos.Components;

namespace Content.Server._ES.Breakable;

public sealed partial class ESBreakableWallSystem : ESSharedBreakableWallSystem
{
    [Dependency] private AirtightSystem _airtight = default!;
    [Dependency] private ConstructionSystem _construction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESChangeConstructionOnBreakComponent, ESBrokenStateChanged>(OnChangeConstructionOnBreak);
    }

    protected override void OnBrokenStateChanged(Entity<ESBreakableWallComponent> ent, ref ESBrokenStateChanged args)
    {
        base.OnBrokenStateChanged(ent, ref args);

        if (TryComp<AirtightComponent>(ent, out var airtight))
            _airtight.SetAirblocked((ent.Owner, airtight), !args.Broken);
    }

    private void OnChangeConstructionOnBreak(Entity<ESChangeConstructionOnBreakComponent> ent, ref ESBrokenStateChanged args)
    {
        if (!args.Broken)
            return;

        if (!string.IsNullOrEmpty(ent.Comp.Node))
            _construction.ChangeNode(ent, null, ent.Comp.Node);
    }
}
