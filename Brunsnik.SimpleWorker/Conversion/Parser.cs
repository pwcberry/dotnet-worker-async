using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Brunsnik.SimpleWorker.Conversion
{
    public class Parser
    {
        private readonly XDocument document = new(new XElement("Set"));

        public XDocument? Parse(TextReader source)
        {
            var tokens = Tokenizer.Tokenize(source);
            document.Root!.Add(tokens.Select(token => new XElement("Point", new XAttribute("X", token.Item1), new XAttribute("Y", token.Item2))));
            return document;
        }
    }
}
