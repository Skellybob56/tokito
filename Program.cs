
using System.Diagnostics;

namespace Tokito;

internal static class Program
{
    [Flags]
    enum Spacing : byte
    {
        None = 0,
        Pre = 1,
        Post = 2,
        Bracket = 4
    }
    
    // todo: add options for how lossy encoding should be
    static byte[] Tokenize(string text, string[] words, (char character, Spacing spacing)[] punctuation)
    {
        static byte ParseCurrentWord(string currentWord, string[] words)
        {
            // todo: add safety for cases where the word is unknown
            return (byte) words.IndexOf(currentWord);
        }
        static byte ParsePunctuation(char character, int wordsLength, (char character, Spacing spacing)[] punctuation)
        {
            // todo: add safety for cases where the symbol is unknown
            return (byte)(wordsLength + Array.FindIndex(punctuation, p => p.character == character));
        }

        if (words.Length + punctuation.Length > byte.MaxValue)
        { throw new ArgumentException("The current format does not allow for more than 256 total words and punctuation", nameof(words) + ", " + nameof(punctuation)); }

        List<byte> tokens = [];
        
        string currentWord = "";
        foreach (char character in text)
        {
            if (character >= 'a' && character <= 'z')
            {
                currentWord += character;
            }
            else
            {
                if (currentWord != "")
                {
                    tokens.Add(ParseCurrentWord(currentWord, words));
                    currentWord = "";
                }
                if (punctuation.Any(p => p.character == character))
                {
                    tokens.Add(ParsePunctuation(character, words.Length, punctuation));
                }
            }
        }
        if (currentWord != "")
        { tokens.Add(ParseCurrentWord(currentWord, words)); }

        return tokens.ToArray();
    }

    static string Detokenize(byte[] tokens, string[] words, (char character, Spacing spacing)[] punctuation)
    {
        string output = "";

        foreach (byte index in tokens)
        {
            // todo: add safety to ensure that the index isn't greater than the length of punctuation and words combined
            // todo: add automatic spacing to words and punctuation. this can be done by having each punctuation mark also store an enum for what spacing it uses.
            output += index < words.Length ? words[index] : punctuation[index - words.Length].character;
            output += ' '; // temp spacer
        }

        return output;
    }

    static (byte[] tokens, (byte p1, byte p2)[] pairs) PairEncode(byte[] tokens, int maximumPairCount)
    {
        // todo: implement pseudocode (this currently just applies no encoding)

        List<(byte p1, byte p2)> pairs = [];
        LinkedList<byte> linkedTokens = new(tokens);

        while (true)
        {
            // if there are no spaces for additional pairs: break the loop
            if (pairs.Count == maximumPairCount) { break; }
            Debug.Assert(pairs.Count <= maximumPairCount);

            // find the most common adjacent token pair

            // if the most common pair's frequency is less than three: break the loop
            if (true)
            {
                break;
            }

            // add the pair to a pair list and replace all instances of the pair in the tokens array
        }

        return (linkedTokens.ToArray(), pairs.ToArray());
    }

    static byte[] GenerateHeader((byte p1, byte p2)[] pairs)
    {
        if (pairs.Length > byte.MaxValue)
        { throw new ArgumentException("The current format does not allow for more than 255 pairs", nameof(pairs)); }
        byte[] header = new byte[1 + 2*pairs.Length];
        header[0] = (byte)pairs.Length;

        for (int i = 0; i < pairs.Length; i++)
        {
            header[1 + 2*i] = pairs[i].p1;
            header[2 + 2*i] = pairs[i].p2;
        }

        return header;
    }

    static byte[] Compress(byte[] tokens, int maximumPairCount)
    {
        (tokens, (byte p1, byte p2)[] pairs) = PairEncode(tokens, maximumPairCount);
        byte[] header = GenerateHeader(pairs);

        byte[] compressed = new byte[header.Length + tokens.Length];
        header.CopyTo(compressed, 0);
        tokens.CopyTo(compressed, header.Length);

        return compressed;
    }

    static void Main()
    {
        // todo: load these from data files
        string text = "nasin lete\n\nlon tenpo lete la jan mute li tawa kepeken ilo suli.\ntaso mi tawa kepeken noka. mi jo ala e ilo tawa suli. mi sona ala kepeken ilo tawa suli.\nko walo li kama tan sewi la jan pali pi kulupu lawa ma li weka e ko walo tan nasin ilo kepeken tenpo lili. ike la, jan pali sama li weka ala e ko tan nasin pi tawa noka.\nmi jan tawa nanpa wan la mi o pali e nasin kepeken noka mi a!\ntaso tenpo mute la mi jan tawa nanpa wan ala. mi ken tawa lon nasin ni: jan ante li tawa lon tenpo pini.\nmi pilin pona tan ni: jan ante mute li sama mi li tawa noka lon tenpo lete. kulupu lawa li pali sama ni: mi lon ala. taso mi ale li lon li awen tawa kepeken noka a!\n";
        string[] words = ["a", "akesi", "ala", "alasa", "ale", "anpa", "ante", "anu", "awen", "e", "en", "esun", "ijo", "ike", "ilo", "insa", "jaki", "jan", "jelo", "jo", "kala", "kalama", "kama", "kasi", "ken", "kepeken", "kili", "kiwen", "ko", "kon", "kule", "kulupu", "kute", "la", "lape", "laso", "lawa", "len", "lete", "li", "lili", "linja", "lipu", "loje", "lon", "luka", "lukin", "lupa", "ma", "mama", "mani", "mi", "moku", "moli", "monsi", "mu", "mun", "musi", "mute", "nanpa", "nasa", "nasin", "nena", "ni", "nimi", "noka", "o", "olin", "ona", "open", "pakala", "pali", "palisa", "pan", "pana", "pi", "pilin", "pimeja", "pini", "pipi", "poka", "poki", "pona", "pu", "sama", "seli", "selo", "seme", "sewi", "sijelo", "sike", "sin", "sina", "sinpin", "sitelen", "sona", "soweli", "suli", "suno", "supa", "suwi", "tan", "taso", "tawa", "telo", "tenpo", "toki", "tomo", "tu", "unpa", "uta", "utala", "walo", "wan", "waso", "wawa", "weka", "wile"];
        // todo: make SpacedChar struct to improve readability 
        (char character, Spacing spacing)[] punctuation = [('\n', Spacing.None), ('.', Spacing.Post), (',', Spacing.Post), (':', Spacing.Post), ('"', Spacing.Bracket), ('?', Spacing.Post), ('!', Spacing.Post), ('\'', Spacing.Bracket)];

        byte[] tokens = Tokenize(text, words, punctuation);
        byte[] compressed = Compress(tokens, 256 - (words.Length + punctuation.Length));

        Console.WriteLine(Detokenize(tokens, words, punctuation));

        Console.Read(); // pause until enter
    }
}