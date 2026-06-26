using Brunsnik.SimpleWorker.Conversion;

namespace Brunsnik.SimpleWorker.Test;

public class ParserTests
{
    [Fact]
    public void Parse_CreatesSetDocumentWithPointElements()
    {
        var parser = new Parser();
        using var reader = new StringReader("1 2\n3 4");

        var document = parser.Parse(reader);

        Assert.NotNull(document);
        Assert.NotNull(document!.Root);
        Assert.Equal("Set", document.Root!.Name.LocalName);

        var points = document.Root.Elements("Point").ToList();
        Assert.Equal(2, points.Count);
        Assert.Equal("1", points[0].Attribute("X")?.Value);
        Assert.Equal("2", points[0].Attribute("Y")?.Value);
        Assert.Equal("3", points[1].Attribute("X")?.Value);
        Assert.Equal("4", points[1].Attribute("Y")?.Value);
    }
}
