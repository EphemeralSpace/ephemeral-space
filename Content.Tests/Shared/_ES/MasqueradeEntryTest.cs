using System.Collections.Generic;
using Content.Shared._ES.Masks;
using Content.Shared._ES.Masks.Masquerades;
using NUnit.Framework;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown.Value;

namespace Content.Tests.Shared._ES;

[TestFixture]
public sealed class MasqueradeEntryTest
{
    [Test]
    [TestCase("Mask1")]
    [TestCase("-Mask1")]
    [TestCase("Mask1/Mask2")]
    [TestCase("-Mask1/Mask2")]
    [TestCase("Mask1(3)")]
    [TestCase("-Mask1(3)")]
    [TestCase("Mask1/Mask2(3)")]
    [TestCase("-Mask1/Mask2(3)")]
    [TestCase("-Mask1/Mask2/Mask3/Mask4/Mask5(1)")]
    [TestCase("#MaskSet")]
    [TestCase("-#MaskSet")]
    [TestCase("#MaskSet(3)")]
    [TestCase("-#MaskSet(3)")]
    public void ParseEntry(string entry)
    {
        Assert.That(MasqueradeEntry.TryRead(entry, null, out var entryParsed, out _));

        Assert.Multiple(() =>
        {
            Assert.That(entryParsed!.Count, Is.GreaterThan(0));
            Assert.That(entryParsed!.Subtract, Is.EqualTo(entry.StartsWith('-')));
            // Ensure we're working with the types we support, so if someone adds more they gotta fix it.
            Assert.That(entryParsed, Is.TypeOf<MasqueradeEntry.DirectEntry>().Or.TypeOf<MasqueradeEntry.SetEntry>());

            if (entryParsed is MasqueradeEntry.DirectEntry e)
            {
                Assert.That(e.Masks, Is.Not.Empty);
            }
            else if (entryParsed is MasqueradeEntry.SetEntry e2)
            {
                Assert.That(e2.MaskSet, Is.Not.EqualTo(string.Empty));
            }
        });
    }
}
