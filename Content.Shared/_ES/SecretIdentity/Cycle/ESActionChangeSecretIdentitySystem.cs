using Content.Shared.Mind;

namespace Content.Shared._ES.SecretIdentity.Cycle;

/// <summary>
/// This handles the mask change action.
/// </summary>
public sealed partial class ESActionChangeSecretIdentitySystem : EntitySystem
{
    [Dependency] private ESSharedSecretIdentitySystem _secretIdentity = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESActionChangeSecretIdentityEvent>(Handler);
    }

    private void Handler(ESActionChangeSecretIdentityEvent args)
    {
        if (args.Handled)
            return;

        if (!_mind.TryGetMind(args.Performer, out var mind, out var mindComp))
            return;

        _secretIdentity.RemoveMask((mind, mindComp));
        _secretIdentity.ApplyMask((mind, mindComp), args.Mask);

        args.Handled = true;
    }
}
