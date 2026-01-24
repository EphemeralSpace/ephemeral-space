using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared._Citadel.Utilities;

[TypeSerializer]
public sealed class RngSeedSerializer : ITypeSerializer<RngSeed, ValueDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        return RngSeed.TryFromStringAsSerialized(node.Value, out _) ? new ValidatedValueNode(node) : new ErrorNode(node, $"Invalid serialized RngSeed. Failed to parse {node.Value}");
    }

    public RngSeed Read(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<RngSeed>? instanceProvider = null)
    {
        RngSeed.TryFromStringAsSerialized(node.Value, out var rng);
        return rng!.Value;
    }

    public DataNode Write(ISerializationManager serializationManager,
        RngSeed value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return new ValueDataNode(value.ToString());
    }
}
