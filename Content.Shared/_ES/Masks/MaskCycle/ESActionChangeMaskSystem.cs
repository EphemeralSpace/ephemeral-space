using Content.Shared.Mind;

namespace Content.Shared._ES.Masks.MaskCycle;

/// <summary>
/// This handles the mask change action.
/// </summary>
public sealed class ESActionChangeMaskSystem : EntitySystem
{
    [Dependency] private readonly ESSharedMaskSystem _mask = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESActionChangeMaskEvent>(Handler);
    }

    private void Handler(ESActionChangeMaskEvent args)
    {
        if (args.Handled)
            return;

        if (!_mind.TryGetMind(args.Performer, out var mind, out var mindComp))
            return;

        _mask.RemoveMask((mind, mindComp));
        _mask.ApplyMask((mind, mindComp), args.Mask);

        args.Handled = true;
    }
}
