using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Client._ES.Breakable.Components;

[RegisterComponent]
[Access(typeof(ESBrokenVisualsSystem))]
public sealed partial class ESBrokenVisualsComponent : Component
{
    [DataField(required: true)]
    public ResPath BaseRSI;

    [DataField(required: true)]
    public ResPath BrokenRSI;

    [DataField(required: true, customTypeSerializer:typeof(ConstantSerializer<DrawDepth>))]
    public int BaseDrawDepth;

    [DataField(required: true, customTypeSerializer:typeof(ConstantSerializer<DrawDepth>))]
    public int BrokenDrawDepth;
}
