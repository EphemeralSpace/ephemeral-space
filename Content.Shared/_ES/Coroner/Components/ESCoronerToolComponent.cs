using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Coroner.Components;

/// <summary>
/// A tool usable by <see cref="ESCoronerUserComponent"/> that gives information about dead bodies.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedCoronerSystem))]
public sealed partial class ESCoronerToolComponent : Component
{
    [DataField]
    public TimeSpan AnalyzeTime = TimeSpan.FromSeconds(5); // TODO: testing value

    [DataField]
    public EntProtoId ReportPrototype = "Paper";
}

[Serializable, NetSerializable]
public sealed partial class ESCoronerAnalyzeDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone()
    {
        return this;
    }
}
