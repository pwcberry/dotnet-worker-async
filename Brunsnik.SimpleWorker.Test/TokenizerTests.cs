using Brunsnik.SimpleWorker.Conversion;

namespace Brunsnik.SimpleWorker.Test;

public class TokenizerAndParserTests
{
    [Fact]
    public void Tokenize_ReturnsOnlyMatchingLines_AsTokens()
    {
        using var reader = new StringReader("10 20\nignored\n30 40");

        var tokens = Tokenizer.Tokenize(reader).ToList();

        Assert.Equal(2, tokens.Count);
        Assert.Equal(10, tokens[0].Item1);
        Assert.Equal(20, tokens[0].Item2);
        Assert.Equal(30, tokens[1].Item1);
        Assert.Equal(40, tokens[1].Item2);
    }

    [Fact]
    public void Tokenize_IgnoresNonMatchingContent()
    {
        using var reader = new StringReader("no tokens here\nstill nothing\n1 2");

        var tokens = Tokenizer.Tokenize(reader).ToList();

        Assert.Single(tokens);
        Assert.Equal(1, tokens[0].Item1);
        Assert.Equal(2, tokens[0].Item2);
    }
}
