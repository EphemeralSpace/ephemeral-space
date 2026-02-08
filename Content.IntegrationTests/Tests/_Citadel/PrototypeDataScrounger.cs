#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.IntegrationTests.Tests._Citadel;

/// <summary>
///     A helper class for when you need prototype data particularly early, like for test lists.
///     This does not include engine prototypes, nor anything generated at runtime.
/// </summary>
public static class PrototypeDataScrounger
{
    /// <summary>
    ///     Prototype type to ID index.
    /// </summary>
    private static Dictionary<string, List<string>>? PrototypeIndex = null;

    /// <summary>
    ///     Lock used to synchronize access to the prototype index.
    /// </summary>
    private static readonly Lock DataLock = new();

    public static string[] PrototypesOfKind<T>()
        where T : IPrototype
    {
        if (typeof(T).GetCustomAttribute<PrototypeAttribute>() is { Type: { } ty })
            return PrototypesOfKind(ty);

        return PrototypesOfKind(PrototypeUtility.CalculatePrototypeName(typeof(T).Name));
    }

    /// <summary>
    ///     Gets all prototypes of the given string kind.
    /// </summary>
    public static string[] PrototypesOfKind(string kind)
    {
        lock (DataLock)
        {
            if (PrototypeIndex is { } index)
            {
                return index[kind].ToArray();
            }
            else
            {
                Scrounge();

                return PrototypeIndex[kind].ToArray();
            }
        }
    }

    [MemberNotNull(nameof(PrototypeIndex))]
    private static void Scrounge()
    {
        PrototypeIndex = new();
        var resDir = ContentResources();
        DebugTools.Assert(Directory.Exists($"{resDir}/Prototypes"));

        var explorationList = new List<string>() { $"{resDir}/Prototypes" };

        while (explorationList.Count > 0)
        {
            var dir = explorationList.Pop();
            explorationList.AddRange(Directory.EnumerateDirectories(dir));

            foreach (var file in Directory.EnumerateFiles(dir, "*.yml"))
            {
                foreach (var (kind, id) in IndexPrototypesIn(file))
                {
                    // alternate universe where .net has rust's Entry api.
                    if (!PrototypeIndex.TryGetValue(kind, out var list))
                    {
                        PrototypeIndex[kind] = new();
                        list = PrototypeIndex[kind];
                    }

                    list.Add(id);
                }
            }
        }
    }

    private static readonly YamlScalarNode IdNode = new("id");
    private static readonly YamlScalarNode TypeNode = new("type");

    private static IEnumerable<(string, string)> IndexPrototypesIn(string file)
    {
        var stream = new YamlStream();

        stream.Load(File.OpenText(file));

        foreach (var document in stream)
        {
            DebugTools.Assert(document.RootNode is YamlSequenceNode);
            var node = (YamlSequenceNode)document.RootNode;

            foreach (var entry in node.Children)
            {
                DebugTools.Assert(entry is YamlMappingNode);
                var entryMapping = (YamlMappingNode)entry;

                var id = entryMapping[IdNode];
                var type = entryMapping[TypeNode];
                if (entryMapping.TryGetNode("abstract", out YamlScalarNode? aabstract))
                {
                    // TODO: This technically will exclude prototypes that use the abstract field for their own stuff,
                    //       and not for parenting. However no such prototype exists in the game as of writing and solving
                    //       this is mildly nontrivial (need to index all the kinds of prototypes in advance)

                    // We use exact equality to match what serialization does.
                    if (aabstract.Value == "true")
                        continue;
                }


                yield return (((YamlScalarNode)type).Value!, ((YamlScalarNode)id).Value!);
            }
        }
    }

    // Did you know there's no way to find the resources folder in the real filesystem
    // from content? Makes sense, but ough.

    /// <summary>
    ///     Get the full directory path that the executable is located in.
    /// </summary>
    private static string GetExecutableDirectory()
    {
        // TODO: remove this shitty hack, either through making it less hardcoded into shared,
        //   or by making our file structure less spaghetti somehow.
        var assembly = typeof(IResourceManager).Assembly;
        var location = assembly.Location;
        if (location == string.Empty)
        {
            // See https://docs.microsoft.com/en-us/dotnet/api/system.reflection.assembly.location?view=net-5.0#remarks
            // This doesn't apply to us really because we don't do that kind of publishing, but whatever.
            throw new InvalidOperationException("Cannot find path of executable.");
        }

        return Path.GetDirectoryName(location)!;
    }

    /// <summary>
    ///     Turns a relative path from the executable directory into a full path.
    /// </summary>
    private static string ExecutableRelativeFile(string file)
    {
        return Path.GetFullPath(Path.Combine(GetExecutableDirectory(), file));
    }

    private static string FindContentRootDir()
    {
        return "../../";
    }

    private static string ContentResources()
    {
        return ExecutableRelativeFile($"{FindContentRootDir()}Resources");
    }
}
