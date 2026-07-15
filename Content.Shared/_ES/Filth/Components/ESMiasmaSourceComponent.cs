using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._ES.Filth.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ESMiasmaSourceComponent : Component
{
    [DataField]
    public float MolPerSecond = 0.2f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;

    [DataField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(1);
}
