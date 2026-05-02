
using System.Diagnostics;
using System.Text;

namespace Tokito;

internal static class Program
{
	// todo: add options for how lossy encoding should be
	// todo: add capability for encoding losslessly (part of this will be introducing special control characters)
	// todo: consider swapping the order of punctuation and words in the index encoding as punctuation is more stable and more stable tokens should have lower indices
	const int escapeCodeCount = 4; // todo: split tokenizer into its own class to encapsulate this const

	// todo: implement escape codes
	static byte[] Tokenize(string text, (char character, bool spaced)[] punctuation, string[] words)
	{
		if (escapeCodeCount + punctuation.Length + words.Length > byte.MaxValue + 1)
		{ throw new ArgumentException($"The current format does not allow for more than {byte.MaxValue + 1} total escape codes, punctuation and words", nameof(words) + ", " + nameof(punctuation)); }

		static byte ParseCurrentWord(string currentWord, int punctuationLength, string[] words)
		{
			if (words.Contains(currentWord))
			{ return (byte) (escapeCodeCount + punctuationLength + words.IndexOf(currentWord)); }

			throw new NotImplementedException("unknown word");
		}

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
					tokens.Add(ParseCurrentWord(currentWord, punctuation.Length, words));
					currentWord = "";
				}

				if (character == ' ')
				{
					// todo: check if this space is predicted: if not then add the missing space as a token
					// todo: also check if a space is predicted in an area where there should be no space and use some method to supress it. example: 'ona.mi'
				}
				else if (character == '\r') { } // silently ignores \r
				else if (punctuation.Any(p => p.character == character))
				{
					tokens.Add((byte)(escapeCodeCount + Array.FindIndex(punctuation, p => p.character == character)));
				}
				else
				{
					throw new NotImplementedException("unknown symbol"); // causes a feature regression but is more honest. this regression is needed for future lossless encoding
				}
			}
		}
		if (currentWord != "")
		{ tokens.Add(ParseCurrentWord(currentWord, punctuation.Length, words)); }

		return tokens.ToArray();
	}

	static string Detokenize(byte[] tokens, (char character, bool spaced)[] punctuation, string[] words)
	{
		if (escapeCodeCount + punctuation.Length + words.Length > byte.MaxValue + 1)
		{ throw new ArgumentException($"The current format does not allow for more than {byte.MaxValue + 1} total escape codes, punctuation and words", nameof(words) + ", " + nameof(punctuation)); }

		StringBuilder output = new();

		bool spaceBeforeNextWord = false;
		foreach (byte index in tokens)
		{
			bool isWord = index >= escapeCodeCount + punctuation.Length;

			if (index < escapeCodeCount)
			{
				throw new NotImplementedException("Escape codes not yet implemented");
			}
			else if (index < escapeCodeCount + punctuation.Length)
			{
				// todo: add safety to ensure that the index isn't greater or equal to the length of punctuation and words combined with escapeCodeCount
				(char character, bool spaced) currentPunctuation = punctuation[index - escapeCodeCount];

				output.Append(currentPunctuation.character);
				spaceBeforeNextWord = currentPunctuation.spaced;
			}
			else
			{
				if (spaceBeforeNextWord)
				{
					output.Append(' ');
				}
				output.Append(words[index - escapeCodeCount - punctuation.Length]);
				spaceBeforeNextWord = true;
			}
		}

		return output.ToString();
	}

	// todo: make a BytePair struct to simplify syntax - replace `(byte p1, byte p2)`
	static (byte[] tokens, (byte p1, byte p2)[] pairs) PairEncode(byte[] tokens, byte minimumPairIndex)
	{
		if (minimumPairIndex == 0) { throw new ArgumentOutOfRangeException(nameof(minimumPairIndex), "Cannot be zero"); }

		byte maximumPairCount = (byte)(256 - minimumPairIndex);

		List<(byte p1, byte p2)> pairs = [];
		LinkedList<byte> linkedTokens = new(tokens); // perf: this could be replaced with a byte?[] array where removed bytes are just nulled - would improve caching

		{
			int[] pairFrequency = new int[256 * 256]; // warning - large array

			// find the frequency of all adjecent token pairs
			for (LinkedListNode<byte>? node = linkedTokens.First; node is not null && node.Next is not null; node = node.Next)
			{
				int index = node.Value * 256 + node.Next.Value;
				pairFrequency[index]++;
			}

			while (pairs.Count < maximumPairCount)
			{
				// find the most common adjacent token pair
				int mostCommonPairIndex = 0;
				int greatestFrequency = pairFrequency[0];
				for (int i = 1; i < pairFrequency.Length; i++) // perf: this could avoid iterating over the sections known to be empty
				{
					if (greatestFrequency < pairFrequency[i])
					{
						mostCommonPairIndex = i;
						greatestFrequency = pairFrequency[i];
					}
				}

				// if the most common pair's frequency is less than three: break the loop (too infrequent for useful compression)
				if (greatestFrequency < 3) { break; }

				// add the pair to a pair list
				(byte p1, byte p2) mostCommonPair = ((byte)(mostCommonPairIndex >> 8), (byte)mostCommonPairIndex);
				byte pairToken = (byte)(minimumPairIndex + pairs.Count);
				pairs.Add(mostCommonPair);

				// replace all instances of the pair in the tokens array and do a partial update of the frequency table for pairs with relation to one of the bytes that have been updated
				for (LinkedListNode<byte>? node = linkedTokens.First; node is not null && node.Next is not null; node = node.Next)
				{
					if (node.Value == mostCommonPair.p1 && node.Next.Value == mostCommonPair.p2) // pair hit
					{
						// update pairFrequency
						if (node.Previous is not null)
						{
							pairFrequency[node.Previous.Value * 256 + node.Value]--;
							pairFrequency[node.Previous.Value * 256 + pairToken]++;
						}
						if (node.Next.Next is not null)
						{
							pairFrequency[node.Next.Value * 256 + node.Next.Next.Value]--;
							pairFrequency[pairToken * 256 + node.Next.Next.Value]++;
						}
						
						// update linkedTokens with the pair compresison
						node.Value = pairToken;
						linkedTokens.Remove(node.Next); // after this, node.Next may be null
					}
				}
				// zero the frequency of the replaced pair
				pairFrequency[mostCommonPair.p1 * 256 + mostCommonPair.p2] = 0;
			}
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

	static byte[] Decompress(byte[] compressed, byte? minimumPairIndex)
	{
		// if no pairs, just remove the initial 'zero pairs' byte and return
		if (minimumPairIndex is null) { return compressed[1..]; }

		// unpack pairs array
		(byte p1, byte p2)[] pairs = new (byte p1, byte p2)[compressed[0]];
		for (int i = 0; i < pairs.Length; i++)
		{ pairs[i] = (compressed[2*i + 1], compressed[2 * i + 2]); }

		// todo: check if any pairs are invalid (e.g. circularity)

		LinkedList<byte> linkedTokens = new(compressed[(1 + pairs.Length*2)..]); // perf: instead of a linked list, we could check the depth of all pairs and precalcualte final array size and then decompress into this final array - would improve caching

		for (LinkedListNode<byte>? node = linkedTokens.First; node is not null; )
		{
			if (node.Value >= minimumPairIndex)
			{
				(byte p1, byte p2) pair = pairs[node.Value - minimumPairIndex.Value];
				node.Value = pair.p1;
				linkedTokens.AddAfter(node, pair.p2);
			}
			else { node = node.Next; } // only progress when not spliting a pair
		}

		return linkedTokens.ToArray();
	}

	static void Main()
	{
		string textPath = "aseki_laso_en_jan_utala_lipu_wan.txt";
		
		string text = File.ReadAllText(textPath);

		// todo: load these from data files
		string[] words = ["a", "akesi", "ala", "alasa", "ale", "anpa", "ante", "anu", "awen", "e", "en", "esun", "ijo", "ike", "ilo", "insa", "jaki", "jan", "jelo", "jo", "kala", "kalama", "kama", "kasi", "ken", "kepeken", "kili", "kiwen", "ko", "kon", "kule", "kulupu", "kute", "la", "lape", "laso", "lawa", "len", "lete", "li", "lili", "linja", "lipu", "loje", "lon", "luka", "lukin", "lupa", "ma", "mama", "mani", "mi", "moku", "moli", "monsi", "mu", "mun", "musi", "mute", "nanpa", "nasa", "nasin", "nena", "ni", "nimi", "noka", "o", "olin", "ona", "open", "pakala", "pali", "palisa", "pan", "pana", "pi", "pilin", "pimeja", "pini", "pipi", "poka", "poki", "pona", "pu", "sama", "seli", "selo", "seme", "sewi", "sijelo", "sike", "sin", "sina", "sinpin", "sitelen", "sona", "soweli", "suli", "suno", "supa", "suwi", "tan", "taso", "tawa", "telo", "tenpo", "toki", "tomo", "tu", "unpa", "uta", "utala", "walo", "wan", "waso", "wawa", "weka", "wile"];
		// todo: make SpacedChar struct to improve readability 
		(char character, bool spaced)[] punctuation = [('\n', false), ('.', true), (',', true), (':', true), ('"', false), ('?', true), ('!', true), ('\'', false)];

		byte? minimumPairIndex = (byte)(words.Length + punctuation.Length);
		
		byte[] tokens = Tokenize(text, punctuation, words);
		byte[] compressed = Compress(tokens, minimumPairIndex);

		File.WriteAllBytes($"{textPath}.toki", compressed);

		Console.WriteLine(Detokenize(Decompress(compressed, minimumPairIndex), punctuation, words));

		Console.Read(); // pause until enter
	}
}
