
namespace Tokito;

static partial class TokiCodex
{
	// todo: add options for how lossy encoding should be
	// todo: add capability for encoding losslessly (implement escape codes)
	static LogicalToken[] Tokenize(string text)
	{
		static CharToken[] WordToCharTokens(string word)
		{
			CharToken[] charTokens = new CharToken[word.Length];

			int i = 0;
			foreach (char character in word)
			{
				charTokens[i] = new(character);
				i++;
			}

			return charTokens;
		}

		static LogicalToken[] ParseWord(string word, int punctuationLength, string[] words)
		{
			// todo: throw if word is null or empty

			if (words.Contains(word))
			{ return [new WordToken(words.IndexOf(word))]; }

			return WordToCharTokens(word);
		}

		List<LogicalToken> tokens = [];
		
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
					tokens.Add(new PunctuationToken(Array.FindIndex(punctuation, p => p.character == character)));
				}
				else
				{
					// add unknown characters as char tokens
					tokens.Add(new CharToken(character));
				}
			}
		}
		if (currentWord != "")
		{ tokens.AddRange(ParseWord(currentWord, punctuation.Length, words)); }

		return tokens.ToArray();
	}
}
