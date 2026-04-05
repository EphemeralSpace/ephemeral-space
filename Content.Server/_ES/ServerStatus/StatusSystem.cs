using Content.Shared.GameTicking;

namespace Content.Server._ES.ServerStatus;

/// <summary>
/// This handles hooking StatusManager into sim a bit.
/// </summary>
public sealed class StatusSystem : EntitySystem
{
    [Dependency] private readonly StatusManager _status = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _status.RerollHostname();
    }
}
