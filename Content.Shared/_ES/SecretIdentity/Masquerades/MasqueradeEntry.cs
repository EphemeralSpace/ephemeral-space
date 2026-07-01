using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using Content.Shared._ES.Utility;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Content.Shared._ES.SecretIdentity.Masquerades;

/// <summary>
///     An entry in a <see cref="MasqueradeRoleSet"/>, describing a set of secret identities, how many times to repeat them,
///     and whether this entry should cancel out some earlier entry.
/// </summary>
/// <remarks>
///     Subtract looks at the value of the entry itself and removes entries that way, it does not attempt to
///     randomly pick secret identities, it's for removing entries entirely.
///     Subtract is not factored in when comparing entries for equality.
/// </remarks>
public abstract record MasqueradeEntry(int Count, bool Subtract) : IMergeable<MasqueradeEntry>
{
    /// <summary>
    ///     Merges this entry with the other entry, accounting for subtraction from the righthand side.
    ///     If this entry was entirely cancelled out, returns false.
    /// </summary>
    public bool Merge(MasqueradeEntry rhs)
    {
        DebugTools.AssertEqual(this.GetType(), rhs.GetType());
        DebugTools.AssertEqual(this.Subtract, false);

        if (rhs.Subtract)
        {
            Count -= rhs.Count;
            if (Count < 0)
            {
                Count = int.Abs(Count);
                Subtract = true; // Gets better error info if we do this. It'll be caught.
            }
        }
        else
        {
            Count += rhs.Count;
        }

        return Count != 0;
    }

    /// <summary>
    ///     An entry containing a set of unweighted secret identities and how many times to repeat them.
    /// </summary>
    public sealed record DirectEntry(IReadOnlySet<ProtoId<ESSecretIdentityPrototype>> SecretIdentities, int Count, bool Subtract)
        : MasqueradeEntry(Count, Subtract)
    {
        public override List<ProtoId<ESSecretIdentityPrototype>> PickSecretIdentities(IRobustRandom random, IPrototypeManager proto)
        {
            DebugTools.Assert(!Subtract, "Subtractive entries shouldn't ever be picked from.");

            var list = new List<ProtoId<ESSecretIdentityPrototype>>(Count);

            for (var i = 0; i < Count; i++)
            {
                list.Add(random.Pick(SecretIdentities));
            }

            return list;
        }
        public override string ToString()
        {
            return base.ToString();
        }
    }

    /// <summary>
    ///     An entry pointing to a secret identity set prototype, and how many times to repeat it.
    /// </summary>
    public sealed record SetEntry(ProtoId<ESSecretIdentitySetPrototype> SecretIdentitySet, int Count, bool Subtract)
        : MasqueradeEntry(Count, Subtract)
    {
        public override List<ProtoId<ESSecretIdentityPrototype>> PickSecretIdentities(IRobustRandom random, IPrototypeManager proto)
        {
            var set = proto.Index(SecretIdentitySet);

            return set.Pick(random, Count);
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    // Magic regex to match everything in an unresolved entry.
    // Not GeneratedRegex because I don't think that works on client.
    private static Regex _entryRegex =
            new(
            @"^(?'subtractive'-)?(\#(?'secretidentityset'[a-zA-Z0-9]+)|(?'secretidentityn'[a-zA-Z0-9]+)(/(?'secretidentityn'[a-zA-Z0-9]+))*)(\((?'count'[0-9]*)\))?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture
            );

    public static bool TryRead(string node, IPrototypeManager? proto, [NotNullWhen(true)] out MasqueradeEntry? action, [NotNullWhen(false)] out string? error)
    {
        var match = _entryRegex.Match(node);

        if (!match.Success)
        {
            action = null;
            error = "Entry didn't match syntax, check for typos?";
            return false;
        }

        var count = 1;
        var subtractive = false;

        if (match.Groups["count"].Captures is [var capture])
        {
            count = int.Parse(capture.ValueSpan);

            if (count == 0)
            {
                action = null;
                error = "Secret identity entry count cannot be 0.";
                return false;
            }
        }

        if (match.Groups["subtractive"].Length == 1)
        {
            subtractive = true;
        }

        if (match.Groups["secretidentityn"].Length > 0)
        {
            var secretIdentities = new HashSet<ProtoId<ESSecretIdentityPrototype>>();

            foreach (Capture entry in match.Groups["secretidentityn"].Captures)
            {
                var value = entry.Value; // This, despite being a property, allocates a new string every time.

                if ((!proto?.TryIndex<ESSecretIdentityPrototype>(value, out _)) ?? false)
                {
                    action = null;
                    error = $"{value} isn't a valid secret identity.";
                    return false;
                }

                secretIdentities.Add(new(value));
            }

            error = null;
            action = new DirectEntry(secretIdentities, count, subtractive);
            return true;
        }
        else if (match.Groups["secretidentityset"].Captures is [{Value: var secretIdentitySetCapture}])
        {
            if ((!proto?.TryIndex<ESSecretIdentitySetPrototype>(secretIdentitySetCapture, out _)) ?? false)
            {
                action = null;
                error = $"{secretIdentitySetCapture} isn't a valid secret identity set.";
                return false;
            }

            error = null;
            action = new SetEntry(secretIdentitySetCapture, count, subtractive);
            return true;
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public abstract List<ProtoId<ESSecretIdentityPrototype>> PickSecretIdentities(IRobustRandom random, IPrototypeManager proto);

    public int Count { get; private set; } = Count;
    public bool Subtract { get; private set; } = Subtract;

    // should look identical to what's in yaml but i don't feel like testing that atm.
    // Notably, the output order for secret identities in a direct set is *arbitrary*, making this unsuitable for serialization.
    // This also always outputs a count, regardless of how it was originally written. `Identity(1)` is equivalent to `Identity`,
    // this doesn't care and always outputs `Identity(1)`.
    public override string ToString()
    {
        var builder = new StringBuilder();

        if (Subtract)
            builder.Append('-');

        switch (this)
        {
            case DirectEntry direct:
            {
                var first = true;
                foreach (var secretIdentity in direct.SecretIdentities)
                {
                    if (!first)
                        builder.Append('/');

                    builder.Append(secretIdentity.ToString());

                    first = false;
                }
                break;
            }
            case SetEntry set:
            {
                builder.Append('#');
                builder.Append(set.SecretIdentitySet.ToString());
                break;
            }
            default:
                throw new NotImplementedException();
        }

        builder.Append('(');
        builder.Append(Count);
        builder.Append(')');

        return builder.ToString();
    }
}

[TypeSerializer]
internal sealed partial class MasqueradeEntrySerializer : ITypeSerializer<MasqueradeEntry, ValueDataNode>,
    ITypeSerializer<MasqueradeEntry.DirectEntry, ValueDataNode>,
    ITypeSerializer<MasqueradeEntry.SetEntry, ValueDataNode>
{
    [Dependency] private IPrototypeManager _proto = default!;

    public ValidationNode Validate(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        if (MasqueradeEntry.TryRead(node.Value, _proto, out _, out var error))
        {
            return new ValidatedValueNode(node);
        }
        else
        {
            return new ErrorNode(node, error);
        }
    }

    public MasqueradeEntry Read(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<MasqueradeEntry>? instanceProvider = null)
    {
        MasqueradeEntry.TryRead(node.Value, _proto, out var value, out var error);

        if (error is not null)
            throw new Exception(error);

        return value!;
    }

    public DataNode Write(ISerializationManager serializationManager,
        MasqueradeEntry value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        throw new NotImplementedException();
    }

    public MasqueradeEntry.DirectEntry Read(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<MasqueradeEntry.DirectEntry>? instanceProvider = null)
    {
        return (MasqueradeEntry.DirectEntry)((ITypeSerializer<MasqueradeEntry, ValueDataNode>)this).Read(serializationManager,
            node,
            dependencies,
            hookCtx,
            context,
            instanceProvider);
    }

    public DataNode Write(ISerializationManager serializationManager,
        MasqueradeEntry.DirectEntry value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        throw new NotImplementedException();
    }

    public MasqueradeEntry.SetEntry Read(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<MasqueradeEntry.SetEntry>? instanceProvider = null)
    {
        return (MasqueradeEntry.SetEntry)((ITypeSerializer<MasqueradeEntry, ValueDataNode>)this).Read(serializationManager,
            node,
            dependencies,
            hookCtx,
            context,
            instanceProvider);
    }

    public DataNode Write(ISerializationManager serializationManager,
        MasqueradeEntry.SetEntry value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        throw new NotImplementedException();
    }
}

