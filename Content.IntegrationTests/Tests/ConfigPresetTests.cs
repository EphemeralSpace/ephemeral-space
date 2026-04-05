using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Entry;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;

namespace Content.IntegrationTests.Tests;

[TestFixture]
public sealed class ConfigPresetTests : GameTest
{
    /// <summary>
    ///     These cvars are random for us and not intended to be consistent across reloads.
    /// </summary>
    public static readonly HashSet<string> IgnoredCVars =
        [
            CVars.GameHostName.Name
        ];

    [Test]
    public async Task TestLoadAll()
    {
        var pair = Pair;
        var server = pair.Server;

        var resources = server.ResolveDependency<IResourceManager>();
        var config = server.ResolveDependency<IConfigurationManager>();

        await server.WaitPost(() =>
        {
            var originalCVars = new List<(string, object)>();
            var registeredCVars = config.GetRegisteredCVars().Except(IgnoredCVars).ToArray();
            foreach (var cvar in registeredCVars)
            {
                var value = config.GetCVar<object>(cvar);
                originalCVars.Add((cvar, value));
            }

            var originalCVarsStream = new MemoryStream();
            config.SaveToTomlStream(originalCVarsStream, registeredCVars);
            originalCVarsStream.Position = 0;

            var presets = resources.ContentFindFiles(EntryPoint.ConfigPresetsDir);
            Assert.Multiple(() =>
            {
                foreach (var preset in presets)
                {
                    var stream = resources.ContentFileRead(preset);
                    Assert.DoesNotThrow(() => config.LoadDefaultsFromTomlStream(stream));
                }
            });

            config.LoadDefaultsFromTomlStream(originalCVarsStream);

            foreach (var (cvar, value) in originalCVars)
            {
                config.SetCVar(cvar, value);
            }

            foreach (var originalCVar in originalCVars)
            {
                var (name, originalValue) = originalCVar;
                var newValue = config.GetCVar<object>(name);
                var originalValueType = originalValue.GetType();
                var newValueType = newValue.GetType();
                if (originalValueType.IsEnum || newValueType.IsEnum)
                {
                    originalValue = Enum.ToObject(originalValueType, originalValue);
                    newValue = Enum.ToObject(originalValueType, newValue);
                }

                if (originalValueType == typeof(float) || newValueType == typeof(float))
                {
                    originalValue = Convert.ToSingle(originalValue);
                    newValue = Convert.ToSingle(newValue);
                }

                if (!Equals(newValue, originalValue))
                    Assert.Fail($"CVar {name} was not reset to its original value.");
            }
        });
    }
}
