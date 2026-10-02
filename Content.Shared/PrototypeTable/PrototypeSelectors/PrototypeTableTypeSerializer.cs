using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.PrototypeTable.PrototypeSelectors;

[TypeSerializer]
public sealed class PrototypeTableTypeSerializer<T> :
    ITypeReader<PrototypeTableSelector<T>, MappingDataNode>
    where T: class, IPrototype
{
    public ValidationNode Validate(ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        if (node.Has(IdSelector<T>.IdDataFieldTag))
            return serializationManager.ValidateNode<IdSelector<T>>(node, context);
        if (node.Has(AllSelector<T>.DataFieldTag))
            return serializationManager.ValidateNode<AllSelector<T>>(node, context);
        if (node.Has(GroupSelector<T>.DataFieldTag))
            return serializationManager.ValidateNode<GroupSelector<T>>(node, context);
        if (node.Has(EntityNestedSelector.DataFieldTag))
        {
            // jank to get around needing to specify a concrete prototype kind
            if (typeof(T) != typeof(EntityPrototype))
                return new ErrorNode(node, $"Table with prototype kind {nameof(T)} does not yet support nested selectors");
            return serializationManager.ValidateNode<EntityNestedSelector>(node, context);
        }
        if (node.Has(PickSelector<T>.DataFieldTag))
            return serializationManager.ValidateNode<PickSelector<T>>(node, context);

        return new ErrorNode(node, "Custom validation not supported! Please specify the type manually!");
    }

    public PrototypeTableSelector<T> Read(ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<PrototypeTableSelector<T>>? instanceProvider = null)
    {
        var type = typeof(PrototypeTableSelector<T>);
        if (node.Has(IdSelector<T>.IdDataFieldTag))
            type = typeof(IdSelector<T>);
        if (node.Has(AllSelector<T>.DataFieldTag))
            type = typeof(AllSelector<T>);
        if (node.Has(GroupSelector<T>.DataFieldTag))
            type = typeof(GroupSelector<T>);
        if (node.Has(EntityNestedSelector.DataFieldTag))
        {
            if (typeof(T) == typeof(EntityPrototype))
                type = typeof(EntityNestedSelector);
        }
        if (node.Has(PickSelector<T>.DataFieldTag))
            type = typeof(PickSelector<T>);

        return (PrototypeTableSelector<T>) serializationManager.Read(type, node, context)!;
    }
}
