using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Changeling;

/// <summary>
/// Action event for Devour, someone has initiated a devour on someone, begin to windup.
/// </summary>
public sealed partial class ChangelingDevourActionEvent : EntityTargetActionEvent
// ES Start
{
    [DataField]
    public bool RequireIncapacitated;
}
// ES End

/// <summary>
/// A windup has either successfully been completed or has been canceled. If successful start the devouring DoAfter.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ChangelingDevourWindupDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// The Consumption DoAfter has either successfully been completed or was canceled.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ChangelingDevourConsumeDoAfterEvent : SimpleDoAfterEvent
// ES Start
{
    [DataField]
    public bool DoHusk;
}

/// <summary>
/// Raised on the person devoured
/// </summary>
[ByRefEvent]
public record struct ESEntityDevouredEvent()
{
    [DataField]
    public ProtoId<PolymorphPrototype> HuskProto = "HuskPolymorph";
}
// ES End
