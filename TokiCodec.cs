
using System.Text;

namespace Tokito;

internal static class TokiCodex
{
    const int escapeCodeCount = 4; // todo: split tokenizer into its own class to encapsulate this const

	// todo: add options for how lossy encoding should be
	// todo: add capability for encoding losslessly (implement escape codes)
	public static byte[] Tokenize(string text, (char character, bool spaced)[] punctuation, string[] words)
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

	public static string Detokenize(byte[] tokens, (char character, bool spaced)[] punctuation, string[] words)
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
}
