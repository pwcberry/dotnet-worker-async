namespace Brunsnik.SimpleWorker.Conversion
{
    public readonly struct Token
    {
        public int Item1 { get; init; }
        public int Item2 { get; init; }

        public Token(string item1, string item2)
        {
            if (int.TryParse(item1, out int parsedItem1))
            {
                Item1 = parsedItem1;
            }

            if (int.TryParse(item2, out int parsedItem2))
            {
                Item2 = parsedItem2;
            }
        }
    }
}
