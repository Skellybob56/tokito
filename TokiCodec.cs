
using System.Buffers.Binary;
using System.Text;

namespace Tokito;

internal static class TokiCodex
{
	static readonly UTF8Encoding strictUTF8Encoding = new(false, true); // do not prepend BOM, do throw on invalid bytes

	static class EscapeCodes
	{
		public const int Count = 4; // must be manually updated
		public const byte TokiSyllableString = 0x00;
		public const byte CapitalizedTokiSyllableString = 0x01;
		public const byte UTF8String = 0x02;
		public const byte UTF16String = 0x03;
	}

	// todo: load these from data files
	// todo: consider adding '-' along with pre-spacing capability to allow it's spacing to work
	// todo: make SpaceableChar struct to improve readability
	static readonly (char character, bool spaced)[] punctuation = [('\n', false), ('.', true), (',', true), (':', true), ('"', false), ('?', true), ('!', true), ('\'', false)];
	static readonly string[] words = ["a", "akesi", "ala", "alasa", "ale", "anpa", "ante", "anu", "awen", "e", "en", "esun", "ijo", "ike", "ilo", "insa", "jaki", "jan", "jelo", "jo", "kala", "kalama", "kama", "kasi", "ken", "kepeken", "kili", "kiwen", "ko", "kon", "kule", "kulupu", "kute", "la", "lape", "laso", "lawa", "len", "lete", "li", "lili", "linja", "lipu", "loje", "lon", "luka", "lukin", "lupa", "ma", "mama", "mani", "mi", "moku", "moli", "monsi", "mu", "mun", "musi", "mute", "nanpa", "nasa", "nasin", "nena", "ni", "nimi", "noka", "o", "olin", "ona", "open", "pakala", "pali", "palisa", "pan", "pana", "pi", "pilin", "pimeja", "pini", "pipi", "poka", "poki", "pona", "sama", "seli", "selo", "seme", "sewi", "sijelo", "sike", "sin", "sina", "sinpin", "sitelen", "sona", "soweli", "suli", "suno", "supa", "suwi", "tan", "taso", "tawa", "telo", "tenpo", "toki", "tomo", "tu", "unpa", "uta", "utala", "walo", "wan", "waso", "wawa", "weka", "wile"];
	
	static readonly int tokenCount = EscapeCodes.Count + punctuation.Length + words.Length; // todo: add safety that ensures that this is <= 256
	public static readonly byte? minimumPairIndex = tokenCount < 256? (byte)tokenCount : null;

	// todo: add options for how lossy encoding should be
	// todo: add capability for encoding losslessly (implement escape codes)
	public static byte[] Tokenize(string text)
	{
		if (EscapeCodes.Count + punctuation.Length + words.Length > byte.MaxValue + 1)
		{ throw new ArgumentException($"The current format does not allow for more than {byte.MaxValue + 1} total escape codes, punctuation and words", nameof(words) + ", " + nameof(punctuation)); }

		static byte[] EncodeUTF8String(string word)
		{
			int dataByteCount = strictUTF8Encoding.GetByteCount(word);

			int neededByteDepth;
			if (dataByteCount < byte.MaxValue) // exclusive to allow for max value sentinel
			{ neededByteDepth = 1; }
			else if (dataByteCount < ushort.MaxValue)
			{ neededByteDepth = 2; }
			else { neededByteDepth = 4; }

			int dataStartIndex = 1 + (2 * neededByteDepth - 1);

			byte[] utf8String = new byte[dataStartIndex + dataByteCount];
			utf8String[0] = EscapeCodes.UTF8String;
			
			// write any needed sentinels
			for (int i = 1; i < neededByteDepth; i++)
			{ utf8String[i] = 0xFF; }

			// write length token
			if(neededByteDepth == 1)
			{ utf8String[neededByteDepth] = (byte)dataByteCount; }
			else if (neededByteDepth == 2)
			{ BinaryPrimitives.WriteUInt16LittleEndian(utf8String.AsSpan(neededByteDepth), (ushort)dataByteCount); }
			else if (neededByteDepth == 4)
			{ BinaryPrimitives.WriteUInt32LittleEndian(utf8String.AsSpan(neededByteDepth), (uint)dataByteCount); }

			strictUTF8Encoding.GetBytes(word, utf8String.AsSpan(dataStartIndex)); // paste the string bytes in at the dataStartIndex

			return utf8String;
		}

		static byte[] ParseWord(string word, int punctuationLength, string[] words)
		{
			// todo: throw if word is null or empty

			if (words.Contains(word))
			{ return [(byte) (EscapeCodes.Count + punctuationLength + words.IndexOf(word))]; }

			// on fail attempt to encode it as a UTF-8 string (as it is alphabetic, it must fit in UTF-8)
			return EncodeUTF8String(word);
		}

		List<byte> tokens = [];
		
		string currentWord = "";
		foreach (char character in text)
		{
			if ((character >= 'a' && character <= 'z') || (character >= 'A' && character <= 'Z'))
			{
				currentWord += character;
			}
			else
			{
				if (currentWord != "")
				{
					tokens.AddRange(ParseWord(currentWord, punctuation.Length, words));
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
					tokens.Add((byte)(EscapeCodes.Count + Array.FindIndex(punctuation, p => p.character == character)));
				}
				else
				{
					throw new NotImplementedException("unknown symbol"); // causes a feature regression but is more honest. this regression is needed for future lossless encoding
				}
			}
		}
		if (currentWord != "")
		{ tokens.AddRange(ParseWord(currentWord, punctuation.Length, words)); }

		return tokens.ToArray();
	}

	public static string Detokenize(byte[] tokens, bool useCRLF)
	{
		if (EscapeCodes.Count + punctuation.Length + words.Length > byte.MaxValue + 1)
		{ throw new ArgumentException($"The current format does not allow for more than {byte.MaxValue + 1} total escape codes, punctuation and words", nameof(words) + ", " + nameof(punctuation)); }

		StringBuilder output = new();

		bool spaceBeforeNextWord = false;
		for (int i = 0; i < tokens.Length; i++)
		{
			byte token = tokens[i];

			if (token < EscapeCodes.Count)
			{
				if (token == EscapeCodes.TokiSyllableString)
				{
					throw new NotImplementedException("toki syllable string decoding not implemented");
				}
				else if (token == EscapeCodes.CapitalizedTokiSyllableString)
				{
					throw new NotImplementedException("capitalized toki syllable string decoding not implemented");
				}
				else if (token == EscapeCodes.UTF8String)
				{
					// load length value
					i++;
					uint length = tokens[i];
					i++;
					if (length == byte.MaxValue)
					{
						length = BinaryPrimitives.ReadUInt16LittleEndian(tokens.AsSpan(i, 2));
						i += 2;
						if (length == ushort.MaxValue)
						{
							length = BinaryPrimitives.ReadUInt32LittleEndian(tokens.AsSpan(i, 4));
							i += 4;
						}
					}

					// todo: consider if it is a problem that this can only get up to an int.MaxValue length string - perhaps put this in the documentation
					// todo: could error on an invalid file (give an informative error message)
					output.Append(strictUTF8Encoding.GetString(tokens, i, (int)length));
					i += (int)length - 1; // move forward to the last item of the string, the for loop will move us onto the next token

					spaceBeforeNextWord = false;
				}
				else if (token == EscapeCodes.UTF16String)
				{
					throw new NotImplementedException("UTF-16 string decoding not implemented");
				}
				else
				{
					throw new InvalidOperationException("unknown escape code");
				}
			}
			else if (token < EscapeCodes.Count + punctuation.Length)
			{
				// todo: add safety to ensure that the index isn't greater or equal to the length of punctuation and words combined with escapeCodeCount
				(char character, bool spaced) currentPunctuation = punctuation[token - EscapeCodes.Count];

				if (useCRLF && currentPunctuation.character == '\n')
				{ output.Append('\r'); }

				output.Append(currentPunctuation.character);
				spaceBeforeNextWord = currentPunctuation.spaced;
			}
			else
			{
				if (spaceBeforeNextWord)
				{
					output.Append(' ');
				}
				output.Append(words[token - EscapeCodes.Count - punctuation.Length]);
				spaceBeforeNextWord = true;
			}
		}

		return output.ToString();
	}
}
