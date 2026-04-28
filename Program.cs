
using System.Diagnostics;
using System.Text;

namespace Tokito;

internal static class Program
{
    enum Spacing : byte
    {
        None,
        Post,
        Bracket
    }
    
    // todo: add options for how lossy encoding should be
    // todo: add capability for encoding losslessly (part of this will be introducing special control characters)
    // todo: consider swapping the order of punctuation and words in the index encoding as punctuation is more stable and more stable tokens should have lower indices
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

        if (words.Length + punctuation.Length > byte.MaxValue + 1) // todo: consider replacing many of these exceptions with debug asserts
        { throw new ArgumentException($"The current format does not allow for more than {byte.MaxValue + 1} total words and punctuation", nameof(words) + ", " + nameof(punctuation)); }

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
        StringBuilder output = new();

        bool spaceBeforeNextWord = false;
        foreach (byte index in tokens)
        {
            bool isWord = index < words.Length;

            if (isWord)
            {
                if (spaceBeforeNextWord)
                {
                    output.Append(' ');
                }
                output.Append(words[index]);
                spaceBeforeNextWord = true;
            }
            else
            {
                // todo: add safety to ensure that the index isn't greater than the length of punctuation and words combined
                (char character, Spacing spacing) currentPunctuation = punctuation[index - words.Length];

                // todo: add spacing logic for Spacing.Bracket
                output.Append(currentPunctuation.character);

                if (currentPunctuation.spacing == Spacing.Post)
                { spaceBeforeNextWord = true; }
                else { spaceBeforeNextWord = false; }
            }
        }

        return output.ToString();
    }

    static (byte[] tokens, (byte p1, byte p2)[] pairs) PairEncode(byte[] tokens, byte minimumPairIndex)
    {
        // todo: implement pseudocode (this currently just applies no encoding)

        if (minimumPairIndex == 0) { throw new ArgumentOutOfRangeException(nameof(minimumPairIndex), "Cannot be zero"); }

        byte maximumPairCount = (byte)(256 - minimumPairIndex);

        List<(byte p1, byte p2)> pairs = [];
        LinkedList<byte> linkedTokens = new(tokens); // todo: consider making this a linked list earlier in the process

        while (true)
        {
            // if there are no spaces for additional pairs: break the loop
            if (pairs.Count == maximumPairCount) { break; }

            // find the frequency of all adjecent token pairs
            // todo: only calculate this once and then use partial updating on pair replacement
            int[] pairFrequency = new int[minimumPairIndex * minimumPairIndex]; // todo: consider a denser format because this array can be huge
            for (LinkedListNode<byte>? node = linkedTokens.First; node is not null && node.Next is not null; node = node.Next)
            {
                int index = node.Value + (minimumPairIndex * node.Next.Value);
                pairFrequency[index]++;
            }

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

    static byte[] Compress(byte[] tokens, byte? minimumPairIndex)
    {
        if (minimumPairIndex is not null)
        {
            (tokens, (byte p1, byte p2)[] pairs) = PairEncode(tokens, minimumPairIndex.Value);
            byte[] header = GenerateHeader(pairs);

            byte[] compressed = new byte[header.Length + tokens.Length];
            header.CopyTo(compressed, 0);
            tokens.CopyTo(compressed, header.Length);

            return compressed;
        }
        else
        {
            byte[] compressed = new byte[tokens.Length + 1];
            compressed[0] = 0;
            tokens.CopyTo(compressed, 1);

            return compressed;
        }
    }

    static void Main()
    {
        string textPath = "nasin_lete.txt";
        
        string text = File.ReadAllText(textPath);

        // todo: load these from data files
        string[] words = ["a", "akesi", "ala", "alasa", "ale", "anpa", "ante", "anu", "awen", "e", "en", "esun", "ijo", "ike", "ilo", "insa", "jaki", "jan", "jelo", "jo", "kala", "kalama", "kama", "kasi", "ken", "kepeken", "kili", "kiwen", "ko", "kon", "kule", "kulupu", "kute", "la", "lape", "laso", "lawa", "len", "lete", "li", "lili", "linja", "lipu", "loje", "lon", "luka", "lukin", "lupa", "ma", "mama", "mani", "mi", "moku", "moli", "monsi", "mu", "mun", "musi", "mute", "nanpa", "nasa", "nasin", "nena", "ni", "nimi", "noka", "o", "olin", "ona", "open", "pakala", "pali", "palisa", "pan", "pana", "pi", "pilin", "pimeja", "pini", "pipi", "poka", "poki", "pona", "pu", "sama", "seli", "selo", "seme", "sewi", "sijelo", "sike", "sin", "sina", "sinpin", "sitelen", "sona", "soweli", "suli", "suno", "supa", "suwi", "tan", "taso", "tawa", "telo", "tenpo", "toki", "tomo", "tu", "unpa", "uta", "utala", "walo", "wan", "waso", "wawa", "weka", "wile"];
        // todo: make SpacedChar struct to improve readability 
        (char character, Spacing spacing)[] punctuation = [('\n', Spacing.None), ('.', Spacing.Post), (',', Spacing.Post), (':', Spacing.Post), ('"', Spacing.Bracket), ('?', Spacing.Post), ('!', Spacing.Post), ('\'', Spacing.Bracket)];

        byte[] tokens = Tokenize(text, words, punctuation);
        byte[] compressed = Compress(tokens, (byte)(words.Length + punctuation.Length));

        File.WriteAllBytes($"{textPath}.toki", compressed);

        Console.WriteLine(Detokenize(tokens, words, punctuation)); // todo: add Decompress function and convert back from the compressed data

        Console.Read(); // pause until enter
    }
}
