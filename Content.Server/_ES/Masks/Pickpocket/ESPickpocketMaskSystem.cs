using Content.Shared._ES.Masks.Pickpocket;
using Content.Shared._ES.Viewcone;

namespace Content.Server._ES.Masks.Pickpocket;

public sealed class ESPickpocketMaskSystem : EntitySystem
{
    [Dependency] private readonly ESViewconeAngleSystem _viewconeAngle = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESPickpocketTargetActionEvent>(OnPickpocketTargetAction);
    }

    private void OnPickpocketTargetAction(ESPickpocketTargetActionEvent args)
    {
        Log.Debug($"In viewcone: {_viewconeAngle.InViewcone(args.Target, args.Performer)}");
    }
}
