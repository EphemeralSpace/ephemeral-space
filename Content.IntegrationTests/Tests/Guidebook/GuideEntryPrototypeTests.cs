using System.Collections.Generic;
using System.Linq;
using Content.Client.Guidebook;
using Content.Client.Guidebook.Richtext;
using Content.IntegrationTests.Fixtures;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Content.IntegrationTests.Utility;
using Content.Shared.Guidebook;
using Robust.Shared.Localization;

namespace Content.IntegrationTests.Tests.Guidebook;

[TestFixture]
[TestOf(typeof(GuidebookSystem))]
[TestOf(typeof(GuideEntryPrototype))]
[TestOf(typeof(DocumentParsingManager))]
public sealed class GuideEntryPrototypeTests : GameTest
{
    private static string[] _guideEntries = GameDataScrounger.PrototypesOfKind<GuideEntryPrototype>();

    [Test]
    [TestCaseSource(nameof(_guideEntries))]
    [Description("Ensures a given guidebook entry is valid, checking the document/etc.")]
    public async Task Validate(string protoKey)
    {
        var pair = Pair;
        var client = pair.Client;
        await client.WaitIdleAsync();
        var protoMan = client.ResolveDependency<IPrototypeManager>();
        var resMan = client.ResolveDependency<IResourceManager>();
        var parser = client.ResolveDependency<DocumentParsingManager>();
        var proto = protoMan.Index<GuideEntryPrototype>(protoKey);

        await client.WaitAssertion(() =>
        {
            using var reader = resMan.ContentFileReadText(proto.Text);
            var text = reader.ReadToEnd();

            Assert.That(parser.TryAddMarkup(new Document(), text), $"Failed to parse the guide entry's document.");
        });
    }

    [Test]
    public async Task NoOrphanGuideEntry()
    {
        var pair = Pair;
        var client = pair.Client;
        await client.WaitIdleAsync();
        var protoMan = client.ResolveDependency<IPrototypeManager>();

        await client.WaitAssertion(() =>
        {
            var orphanGuides = new HashSet<ProtoId<GuideEntryPrototype>>(protoMan
                .EnumeratePrototypes<GuideEntryPrototype>()
                .Select(p => (ProtoId<GuideEntryPrototype>) p.ID));
            foreach (var guide in protoMan.EnumeratePrototypes<GuideEntryPrototype>())
            {
                orphanGuides.ExceptWith(guide.Children);
            }

            Assert.Multiple(() =>
            {
                foreach (var orphan in orphanGuides)
                {
                    Assert.That(protoMan.Index(orphan).Root, $"GuideEntryPrototype {orphan} has no parent but is not specified as a root! Did you forget to add it somewhere?");
                }
            });
        });
    }
}
