using System.Collections.Generic;
using Content.Shared._ES.Masks.Masquerades;
using Content.Shared._ES.Utility;
using NUnit.Framework;

namespace Content.Tests.Shared._ES;

[TestFixture]
public sealed class MqKeyEqualityTest
{
    // I lost my MIND debugging this. Never again. -Kaylie
    [Test]
    public void MqKeyTest()
    {
        // Legally distinct entries as far as C# is concerned...
        Assert.That(MasqueradeEntry.TryRead("Yay/Foo/Hell", null, out var entry1, out _));
        Assert.That(MasqueradeEntry.TryRead("-Yay/Foo/Hell", null, out var entry2, out _));

        var key1 = MasqueradeRoleSet.MqKey.FromEntry(entry1!);
        var key2 = MasqueradeRoleSet.MqKey.FromEntry(entry2!);

        Assert.That(key1, Is.EqualTo(key2));

        Assert.That(key1.GetHashCode(), Is.EqualTo(key2.GetHashCode()));

        var dict = new Dictionary<MasqueradeRoleSet.MqKey, MasqueradeEntry>();

        dict.Add(key1, entry1);
        Assert.That(dict.TryAdd(key2, entry2), Is.False);

        dict.MergeValue(key2, entry2);

        Assert.That(dict, Is.Empty);
    }
}
