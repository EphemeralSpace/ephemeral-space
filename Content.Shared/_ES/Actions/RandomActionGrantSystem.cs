using Content.Shared.Actions;
using Content.Shared.EntityTable;

namespace Content.Shared._ES.Actions;

public sealed class RandomActionGrantSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ESRandomActionGrantComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ESRandomActionGrantComponent> ent, ref MapInitEvent args)
    {
        var Actions = _entityTable.GetSpawns(ent.Comp.Actions);

        foreach (var action in Actions)
        {
            EntityUid? actionEnt = null;
            _actions.AddAction(ent.Owner, ref actionEnt, action);
        }
    }
}
