using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.Shared._ES.Chat;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._ES.Chat;

[TestFixture]
public sealed class ChatTests : GameTest
{
    [Test]
    [Description("Ensures no chat prefixes are repeated between channels")]
    public void EnsureUniquePrefixes()
    {
        var pair = Pair;
        var server = pair.Server;
        var protoMan = server.ProtoMan;

        // TODO: this doesn't cover multi-character overlap
        var usedPrefixes = new Dictionary<string, List<ProtoId<ESChatChannelPrototype>>>();
        foreach (var channel in protoMan.EnumeratePrototypes<ESChatChannelPrototype>())
        {
            foreach (var prefix in channel.Prefixes)
            {
                usedPrefixes.GetOrNew(prefix);
                usedPrefixes[prefix].Add(channel.ID);
            }
        }

        Assert.Multiple(() =>
        {
            foreach (var (prefix, channels) in usedPrefixes)
            {
                Assert.That(channels.Count <= 1,
                    $"Chat channel prefix \'{prefix}\' is used multiple times (in channels {string.Join(", ", channels)})");
            }
        });
    }
}
