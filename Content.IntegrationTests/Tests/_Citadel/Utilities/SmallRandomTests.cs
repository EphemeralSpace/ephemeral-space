using System.IO;
using System.Linq;
using Content.Shared._Citadel.Utilities;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using YamlDotNet.RepresentationModel;

namespace Content.IntegrationTests.Tests._Citadel.Utilities;

[TestFixture]
public sealed class SmallRandomTests
{
    [Test]
    public void IsReproducable()
    {
        Assert.That(RngSeed.TryFromStringAsSeed("awawa", out var myRandom));

        var stringified = myRandom.ToString();

        Assert.That(RngSeed.TryFromStringAsSerialized(stringified, out var andBackAgain));

        var andBackAgainV = andBackAgain!.Value;

        Assert.That(myRandom!.Value.DebugCheckByteEquality(ref andBackAgainV));
    }

    // Ensure we're not just returning a consistent value.
    [Test]
    public void ReasonablyRandom()
    {
        Assert.That(RngSeed.TryFromStringAsSeed("gay!", out var myRandomNullable));
        var myRandom = myRandomNullable!.Value.IntoRandomizer();

        // deliberate as Next() is impure.
#pragma warning disable NUnit2009
        Assert.That(myRandom.Next(), Is.Not.EqualTo(myRandom.Next()));
        Assert.That(myRandom.Next(), Is.Not.EqualTo(myRandom.Next()));
        Assert.That(myRandom.Next(), Is.Not.EqualTo(myRandom.Next()));
#pragma warning restore NUnit2009
    }
}

public sealed class SmallRandomGameTests : GameTest
{
    [SidedDependency(Side.Server)] private ISerializationManager _ser;

    [Test]
    [RunOnSide(Side.Server)]
    public void Serialize()
    {
        Assert.That(RngSeed.TryFromStringAsSeed("colon-three", out var myRandomNullable));
        var myRandom = myRandomNullable!.Value.IntoRandomizer();

        var node = (MappingDataNode)_ser.WriteValue(new SmallRandomTestSer(myRandom));
        var document = new YamlStream {new(node.ToYaml())};
        var writer = new StringWriter();
        document.Save(writer);

        var reader = new StringReader(writer.ToString());

        var readDocument = DataNodeParser.ParseYamlStream(reader).First();

        var mapping = (MappingDataNode) readDocument.Root;

        var parsedMyRandom = _ser.Read<SmallRandomTestSer>(mapping).MyRandom;

        Assert.That(myRandom.DebugCheckByteEquality(parsedMyRandom));
    }
}

[DataDefinition]
public sealed partial class SmallRandomTestSer
{
    [DataField]
    public SmallRandom MyRandom;

    public SmallRandomTestSer(SmallRandom myRandom)
    {
        MyRandom = myRandom;
    }
}
