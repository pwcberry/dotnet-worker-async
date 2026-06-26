using System.Text.RegularExpressions;

namespace Brunsnik.SimpleWorker.Conversion
{
    public static class Tokenizer
    {
        public static IEnumerable<Token> Tokenize(TextReader source)
        {
            while (source.Peek() != -1)
            {
                var line = source.ReadLine() ?? string.Empty;
                var matches = Regex.Matches(line, @"(?<item1>\d+)(?:\.\d*)*\s+(?<item2>\d+)(?:\.\d*)*", RegexOptions.None);
                if (matches.Count > 0 && matches[0].Success)
                {
                    yield return new Token(matches[0].Groups["item1"].Value, matches[0].Groups["item2"].Value);
                }
            }
        }
    }
}
