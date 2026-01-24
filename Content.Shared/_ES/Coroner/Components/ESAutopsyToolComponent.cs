using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Coroner.Components;

/// <summary>
/// A tool usable by <see cref="ESAutopsyUserComponent"/> that gives information about dead bodies.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedCoronerSystem))]
public sealed partial class ESAutopsyToolComponent : Component
{
    [DataField]
    public TimeSpan AutopsyTime = TimeSpan.FromSeconds(30);

    [DataField]
    public SoundSpecifier? AutopsySound = new SoundCollectionSpecifier("PaperScribbles");

    [DataField]
    public EntProtoId ReportPrototype = "Paper";
}

[Serializable, NetSerializable]
public sealed partial class ESAutopsyDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone()
    {
        return this;
    }
}
