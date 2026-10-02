using Content.Shared.PrototypeTable.Conditions;
using Content.Shared.PrototypeTable.ValueSelector;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.PrototypeTable.PrototypeSelectors;

/// <summary>
///     Helper for reading all of the base type stuff since we can't just use a datadefinition normally i think.
/// </summary>
// theres maybe a way to just like. use the base type and read using that? but i couldnt get that to work
[DataDefinition]
public sealed partial class PrototypeTableBaseTypeData
{
    public static readonly string[] Keys = { "rolls", "weight", "prob", "conditions", "requireAll" };

    [DataField] public NumberSelector Rolls = new ConstantNumberSelector(1);
    [DataField] public float Weight = 1;
    [DataField] public double Prob = 1;
    [DataField] public List<PrototypeTableCondition> Conditions = new();
    [DataField] public bool RequireAll = true;
}

// oh my fucking god i hate writing these things
// serializer manually constructs the types to aget around datadefinition generic stuff
[TypeSerializer]
public sealed class PrototypeTableTypeSerializer<T> :
    ITypeReader<PrototypeTableSelector<T>, MappingDataNode>,
    ITypeCopyCreator<PrototypeTableSelector<T>>
    where T : class, IPrototype
{
    private static MappingDataNode BaseData(MappingDataNode node)
    {
        var sub = new MappingDataNode();
        foreach (var key in PrototypeTableBaseTypeData.Keys)
        {
            if (node.TryGet(key, out var value))
                sub.Add(key, value);
        }
        return sub;
    }

    private static NumberSelector ReadAmount(ISerializationManager sm,
        MappingDataNode node,
        SerializationHookContext hookCtx,
        ISerializationContext? context)
    {
        return node.TryGet("amount", out var n)
            ? sm.Read<NumberSelector>(n, hookCtx, context, notNullableOverride: true)
            : new ConstantNumberSelector(1);
    }

    public PrototypeTableSelector<T> Read(ISerializationManager sm,
        MappingDataNode node,
        IDependencyCollection deps,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<PrototypeTableSelector<T>>? instanceProvider = null)
    {
        PrototypeTableSelector<T> selector;

        if (node.TryGet(IdSelector<T>.IdDataFieldTag, out var idNode))
        {
            selector = new IdSelector<T>
            {
                Id = sm.Read<ProtoId<T>>(idNode, hookCtx, context),
                Amount = ReadAmount(sm, node, hookCtx, context),
            };
        }
        else if (node.TryGet(AllSelector<T>.DataFieldTag, out var allNode))
        {
            selector = new AllSelector<T>
            {
                Children = sm.Read<List<PrototypeTableSelector<T>>>(allNode, hookCtx, context, notNullableOverride: true),
            };
        }
        else if (node.TryGet(GroupSelector<T>.DataFieldTag, out var groupNode))
        {
            selector = new GroupSelector<T>
            {
                Children = sm.Read<List<PrototypeTableSelector<T>>>(groupNode, hookCtx, context, notNullableOverride: true),
            };
        }
        else if (node.TryGet(PickSelector<T>.DataFieldTag, out var pickNode))
        {
            selector = new PickSelector<T>
            {
                Child = sm.Read<PrototypeTableSelector<T>>(pickNode, hookCtx, context, notNullableOverride: true),
                Amount = ReadAmount(sm, node, hookCtx, context),
            };
        }
        else if (typeof(T) == typeof(EntityPrototype) && node.Has(EntityNestedSelector.DataFieldTag))
        {
            // non generic to get around the fact that we have to index the table for it to actually work
            // so we just need separate selectors for each type that wants nested tables / prototype-ified tables. sorry
            return (PrototypeTableSelector<T>) (object) sm.Read<EntityNestedSelector>(node, hookCtx, context, notNullableOverride: true)!;
        }
        else
        {
            throw new InvalidOperationException(
                $"Unknown selector node for table of {typeof(T).Name}");
        }

        var common = sm.Read<PrototypeTableBaseTypeData>(BaseData(node), hookCtx, context, notNullableOverride: true);
        selector.Rolls = common.Rolls;
        selector.Weight = common.Weight;
        selector.Prob = common.Prob;
        selector.Conditions = common.Conditions;
        selector.RequireAll = common.RequireAll;
        return selector;
    }

    public ValidationNode Validate(ISerializationManager sm,
        MappingDataNode node,
        IDependencyCollection deps,
        ISerializationContext? context = null)
    {
        var fields = new Dictionary<ValidationNode, ValidationNode>();

        void Add(string key, ValidationNode value)
            => fields.Add(new ValidatedValueNode(new ValueDataNode(key)), value);

        if (node.TryGet(IdSelector<T>.IdDataFieldTag, out var idNode))
        {
            Add(IdSelector<T>.IdDataFieldTag, sm.ValidateNode<ProtoId<T>>(idNode, context));
            if (node.TryGet("amount", out var amount))
                Add("amount", sm.ValidateNode<NumberSelector>(amount, context));
        }
        else if (node.TryGet(AllSelector<T>.DataFieldTag, out var allNode))
        {
            Add(AllSelector<T>.DataFieldTag, sm.ValidateNode<List<PrototypeTableSelector<T>>>(allNode, context));
        }
        else if (node.TryGet(GroupSelector<T>.DataFieldTag, out var groupNode))
        {
            Add(GroupSelector<T>.DataFieldTag, sm.ValidateNode<List<PrototypeTableSelector<T>>>(groupNode, context));
        }
        else if (node.TryGet(PickSelector<T>.DataFieldTag, out var pickNode))
        {
            Add(PickSelector<T>.DataFieldTag, sm.ValidateNode<PrototypeTableSelector<T>>(pickNode, context));
            if (node.TryGet("amount", out var amount))
                Add("amount", sm.ValidateNode<NumberSelector>(amount, context));
        }
        else if (node.Has(EntityNestedSelector.DataFieldTag))
        {
            if (typeof(T) != typeof(EntityPrototype))
                return new ErrorNode(node, $"Table with prototype kind {typeof(T).Name} does not yet support nested selectors");
            return sm.ValidateNode<EntityNestedSelector>(node, context);
        }
        else
        {
            return new ErrorNode(node, $"Unknown selector for table of {typeof(T).Name}");
        }

        Add("common", sm.ValidateNode<PrototypeTableBaseTypeData>(BaseData(node), context));

        return new ValidatedMappingNode(fields);
    }

    // this is what protoid does sooo
    public PrototypeTableSelector<T> CreateCopy(ISerializationManager sm,
        PrototypeTableSelector<T> source,
        IDependencyCollection deps,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        return source;
    }
}
