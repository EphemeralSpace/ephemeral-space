using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Storage;

namespace Content.Shared._Offbrand.Storage;

public sealed class StorageOpenableSystem : EntitySystem
{
    [Dependency] private readonly OpenableSystem _openable = default!;

    public override void Initialize()
    {

        base.Initialize();

        SubscribeLocalEvent<StorageOpenableComponent, StorageInteractAttemptEvent>(OnStorageInteractAttempt);
    }

    private void OnStorageInteractAttempt(Entity<StorageOpenableComponent> ent, ref StorageInteractAttemptEvent args)
    {
        if (_openable.IsOpen(ent) || _openable.TryOpen(ent))
            return;

        args.Cancelled = true;
    }
}
