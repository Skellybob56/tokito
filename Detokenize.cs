
using System.Buffers.Binary;
using System.Text;

namespace Tokito;

static partial class TokiCodex
{
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