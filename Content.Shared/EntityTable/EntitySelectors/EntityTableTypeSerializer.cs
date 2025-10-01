using Content.Shared._ES.EntityTable.EntitySelectors;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.EntityTable.EntitySelectors;

[TypeSerializer]
public sealed class EntityTableTypeSerializer :
    ITypeReader<EntityTableSelector, MappingDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        if (node.Has(EntSelector.IdDataFieldTag))
            return serializationManager.ValidateNode<EntSelector>(node, context);
// ES START
        if (node.Has(ESAllSelector.DataFieldTag))
            return serializationManager.ValidateNode<ESAllSelector>(node, context);
        if (node.Has(ESGroupSelector.DataFieldTag))
            return serializationManager.ValidateNode<ESGroupSelector>(node, context);
        if (node.Has(ESNestedSelector.DataFieldTag))
            return serializationManager.ValidateNode<ESNestedSelector>(node, context);
        if (node.Has(ESPickSelector.DataFieldTag))
            return serializationManager.ValidateNode<ESPickSelector>(node, context);
// ES END

        return new ErrorNode(node, "Custom validation not supported! Please specify the type manually!");
    }

    public EntityTableSelector Read(ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<EntityTableSelector>? instanceProvider = null)
    {
        var type = typeof(EntityTableSelector);
        if (node.Has(EntSelector.IdDataFieldTag))
            type = typeof(EntSelector);
// ES START
        if (node.Has(ESAllSelector.DataFieldTag))
            type = typeof(ESAllSelector);
        if (node.Has(ESGroupSelector.DataFieldTag))
            type = typeof(ESGroupSelector);
        if (node.Has(ESNestedSelector.DataFieldTag))
            type = typeof(ESNestedSelector);
        if (node.Has(ESPickSelector.DataFieldTag))
            type = typeof(ESPickSelector);
// ES END
        return (EntityTableSelector) serializationManager.Read(type, node, context)!;
    }
}
