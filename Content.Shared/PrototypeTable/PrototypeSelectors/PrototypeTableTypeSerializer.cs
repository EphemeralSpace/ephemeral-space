using Content.Shared._ES.PrototypeTable.PrototypeSelectors;
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
// ES START
        if (node.Has(ESAllSelector<T>.DataFieldTag))
            return serializationManager.ValidateNode<ESAllSelector<T>>(node, context);
        if (node.Has(ESGroupSelector<T>.DataFieldTag))
            return serializationManager.ValidateNode<ESGroupSelector<T>>(node, context);
        if (node.Has(ESEntityNestedSelector.DataFieldTag))
        {
            // jank to get around needing to specify a concrete prototype kind
            if (typeof(T) != typeof(EntityPrototype))
                return new ErrorNode(node, $"Table with prototype kind {nameof(T)} does not yet support nested selectors");
            return serializationManager.ValidateNode<ESEntityNestedSelector>(node, context);
        }
        if (node.Has(ESPickSelector<T>.DataFieldTag))
            return serializationManager.ValidateNode<ESPickSelector<T>>(node, context);
// ES END

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
// ES START
        if (node.Has(ESAllSelector<T>.DataFieldTag))
            type = typeof(ESAllSelector<T>);
        if (node.Has(ESGroupSelector<T>.DataFieldTag))
            type = typeof(ESGroupSelector<T>);
        if (node.Has(ESEntityNestedSelector.DataFieldTag))
        {
            if (typeof(T) == typeof(EntityPrototype))
                type = typeof(ESEntityNestedSelector);
        }
        if (node.Has(ESPickSelector<T>.DataFieldTag))
            type = typeof(ESPickSelector<T>);
// ES END
        return (PrototypeTableSelector<T>) serializationManager.Read(type, node, context)!;
    }
}
