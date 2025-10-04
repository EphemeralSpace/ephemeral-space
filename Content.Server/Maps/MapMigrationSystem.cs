using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

namespace Content.Server.Maps;

/// <summary>
///     Performs basic map migration operations by listening for engine <see cref="MapLoaderSystem"/> events.
/// </summary>
public sealed class MapMigrationSystem : EntitySystem
{
#pragma warning disable CS0414
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
#pragma warning restore CS0414
    [Dependency] private readonly IResourceManager _resMan = default!;

    private const string MigrationFile = "/migration.yml";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BeforeEntityReadEvent>(OnBeforeReadEvent);

#if DEBUG
// ES START
        if (!TryGetMigrations(out var mappings))
            return;

        // Verify that all of the entries map to valid entity prototypes.
        foreach (var node in mappings.Values)
        {
            var newId = (node);
// ES END
            if (!string.IsNullOrEmpty(newId) && newId != "null")
                DebugTools.Assert(_protoMan.HasIndex<EntityPrototype>(newId), $"{newId} is not an entity prototype.");
        }
#endif
    }

    private bool TryReadFile([NotNullWhen(true)] out MappingDataNode? mappings)
    {
        mappings = null;
        var path = new ResPath(MigrationFile);
        if (!_resMan.TryContentFileRead(path, out var stream))
            return false;

        using var reader = new StreamReader(stream, EncodingHelpers.UTF8);
        var documents = DataNodeParser.ParseYamlStream(reader).FirstOrDefault();

        if (documents == null)
            return false;

        mappings = (MappingDataNode) documents.Root;
        return true;
    }

// ES START
    public bool TryGetMigrations(out Dictionary<string, string> migrations)
    {
        migrations = new Dictionary<string, string>();
        if (!TryReadFile(out var mappings))
            return false;

        var parsedMappings = new Dictionary<string, string>();
        foreach (var (key, value) in mappings)
        {
            if (value is not ValueDataNode valueNode)
                continue;

            parsedMappings.Add(key, valueNode.Value);
        }

        foreach (var key in parsedMappings.Keys)
        {
            var currentMapping = parsedMappings[key];
            while (parsedMappings.TryGetValue(currentMapping, out currentMapping))
            {
                parsedMappings[key] = currentMapping;
            }
        }

        migrations = parsedMappings;
        return true;
    }

    private void OnBeforeReadEvent(BeforeEntityReadEvent ev)
    {
        if (!TryGetMigrations(out var mappings))
            return;

        foreach (var (key, value) in mappings)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "null")
                ev.DeletedPrototypes.Add(key);
            else
                ev.RenamedPrototypes.Add(key, value);
// ES END
        }
    }
}
