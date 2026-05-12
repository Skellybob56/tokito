
using System.Collections.ObjectModel;

namespace Tokito;

static partial class TokiCodex
{
	// todo: add options for how lossy encoding should be
	// todo: add capability for encoding losslessly (implement escape codes)
	static SerializableToken[] Tokenize(string text)
	{
		List<LogicalToken> unspacedTokens = [];
		
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
					unspacedTokens.AddRange(ParseWord(currentWord, punctuation.Length, words));
					currentWord = "";
				}

				if (character == ' ')
				{
					unspacedTokens.Add(new ExplicitSpaceToken());
					// todo: check if this space is predicted: if not then add the missing space as a token
					// todo: also check if a space is predicted in an area where there should be no space and supress it. example: 'ona.mi'
				}
				else if (character == '\r') { } // silently ignores \r
				else if (punctuation.Any(p => p.character == character))
				{
					int punctuationIndex = Array.FindIndex(punctuation, p => p.character == character);
					unspacedTokens.Add(new PunctuationToken(punctuationIndex));
				}
				else
				{
					// add unknown characters as char tokens
					unspacedTokens.Add(new CharToken(character));
				}
			}
		}
		if (currentWord != "")
		{ unspacedTokens.AddRange(ParseWord(currentWord, punctuation.Length, words)); }

		List<SerializableToken> serializableTokens = PredictSpaces(unspacedTokens.AsReadOnly());

		return serializableTokens.ToArray();

		static SerializableToken[] ParseWord(string word, int punctuationLength, string[] words)
		{
			// todo: throw if word is null or empty

			if (words.Contains(word))
			{ return [new WordToken(words.IndexOf(word))]; }

			return WordToCharTokens(word);
		
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
		}

		static List<SerializableToken> PredictSpaces(ReadOnlyCollection<LogicalToken> unspacedTokens)
		{
			List<SerializableToken> serializableTokens = new(unspacedTokens.Count);

			bool spaceBeforeNextWord = false;
			bool justPredictedSpace = false;
			for (int i = 0; i < unspacedTokens.Count; i++)
			{
				LogicalToken token = unspacedTokens[i];
				if (token is SerializableToken serializableToken)
				{
					if (spaceBeforeNextWord && !justPredictedSpace && serializableToken.Word())
					{
						// a space was predicted that didn't occour
						serializableTokens.Add(new SpaceSupressor());
					}
					serializableTokens.Add(serializableToken);
					spaceBeforeNextWord = serializableToken.Spaced();
					justPredictedSpace = false;
				}
				else if (token is ExplicitSpaceToken)
				{
					if (spaceBeforeNextWord && i < unspacedTokens.Count - 1 && unspacedTokens[i + 1].Word())
					{
						justPredictedSpace = true;
					}
					else
					{
						serializableTokens.Add(new CharToken(' '));
						spaceBeforeNextWord = false;
						justPredictedSpace = false;
					}
				}
			}

			return serializableTokens;
		}
	}
}
