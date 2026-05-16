
using System.Text;

namespace Tokito.Backend;

static partial class TokiCodex
{
	static string Detokenize(SerializableToken[] tokens)
	{
		StringBuilder output = new();

		for (int i = 0; i < tokens.Length; i++)
		{
			SerializableToken token = tokens[i];

			if (i > 0 && tokens[i-1].Spaced() && token.Word())
			{
				output.Append(' ');
			}

			if (token is WordToken wordToken)
			{
				output.Append(words[wordToken.WordIndex]);

			}
			else if (token is PunctuationToken puncToken)
			{
				char puncChar = punctuation[puncToken.PunctuationIndex].Character;
				output.Append(puncChar);
			}
			else if (token is CharToken charToken)
			{
				output.Append(charToken.Value);
			}
		}

		return output.ToString();
	}
}
