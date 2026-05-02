
using System.Text;

namespace Tokito;

internal static class TokiCodex
{
	// todo: load these from data files
	// todo: make SpaceableChar struct to improve readability
    const int escapeCodeCount = 4;
    static readonly (char character, bool spaced)[] punctuation = [('\n', false), ('.', true), (',', true), (':', true), ('"', false), ('?', true), ('!', true), ('\'', false)];
	static readonly string[] words = ["a", "akesi", "ala", "alasa", "ale", "anpa", "ante", "anu", "awen", "e", "en", "esun", "ijo", "ike", "ilo", "insa", "jaki", "jan", "jelo", "jo", "kala", "kalama", "kama", "kasi", "ken", "kepeken", "kili", "kiwen", "ko", "kon", "kule", "kulupu", "kute", "la", "lape", "laso", "lawa", "len", "lete", "li", "lili", "linja", "lipu", "loje", "lon", "luka", "lukin", "lupa", "ma", "mama", "mani", "mi", "moku", "moli", "monsi", "mu", "mun", "musi", "mute", "nanpa", "nasa", "nasin", "nena", "ni", "nimi", "noka", "o", "olin", "ona", "open", "pakala", "pali", "palisa", "pan", "pana", "pi", "pilin", "pimeja", "pini", "pipi", "poka", "poki", "pona", "pu", "sama", "seli", "selo", "seme", "sewi", "sijelo", "sike", "sin", "sina", "sinpin", "sitelen", "sona", "soweli", "suli", "suno", "supa", "suwi", "tan", "taso", "tawa", "telo", "tenpo", "toki", "tomo", "tu", "unpa", "uta", "utala", "walo", "wan", "waso", "wawa", "weka", "wile"];
    
    static readonly int tokenCount = escapeCodeCount + punctuation.Length + words.Length; // todo: add safety that ensures that this is <= 256
    public static readonly byte? minimumPairIndex = tokenCount < 256? (byte)tokenCount : null;

    // todo: add options for how lossy encoding should be
	// todo: add capability for encoding losslessly (implement escape codes)
	public static byte[] Tokenize(string text)
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

	public static string Detokenize(byte[] tokens, bool useCRLF)
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
				output.Append(words[index - escapeCodeCount - punctuation.Length]);
				spaceBeforeNextWord = true;
			}
		}

		return output.ToString();
	}
}
