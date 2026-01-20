using Content.Shared.Actions;

namespace Content.Shared._ES.Changeling;

public sealed partial class ESChangelingStatisEvent : InstantActionEvent
{
}

/// <summary>
/// Raised on an entity when Its mind is attempting to ghost out.
/// </summary>
[ByRefEvent]
public record struct ESGhostAttemptEvent(EntityUid Mind, bool Cancelled = false);

