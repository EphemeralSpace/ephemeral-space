using Content.Server._ES.Masks.Hemophage.Components;
using Content.Server.Mind;
using Robust.Shared.Physics.Events;

namespace Content.Server._ES.Masks.Hemophage;

public sealed class ESMaskConvertOnCollideSystem : EntitySystem
{
    [Dependency] private readonly ESMaskSystem _mask = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESMaskConvertOnCollideComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(Entity<ESMaskConvertOnCollideComponent> ent, ref StartCollideEvent args)
    {
        if (!_mind.TryGetMind(args.OtherEntity, out var mind))
            return;

        if (_mask.GetTroupeOrNull(args.OtherEntity) == ent.Comp.IgnoreTroupe)
            return;

        _mask.ChangeMask(mind.Value, ent.Comp.Mask);
    }
}
