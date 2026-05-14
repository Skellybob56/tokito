
using System.Text;

namespace Tokito.Backend;

static partial class TokiCodex
{
	static string Detokenize(SerializableToken[] tokens, bool useCRLF)
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

				if (useCRLF && puncChar == '\n')
				{ output.Append('\r'); }

				output.Append(puncChar);
			}
			else if (token is CharToken charToken)
			{
				if (useCRLF && charToken.Value == '\n')
				{ output.Append('\r'); }
				
				output.Append(charToken.Value);
			}
		}

		return output.ToString();
	}
}