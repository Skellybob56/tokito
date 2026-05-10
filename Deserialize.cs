
using System.Buffers.Binary;

namespace Tokito;

static partial class TokiCodex
{
	static SerializableToken[] Deserialize(byte[] data)
	{
		List<SerializableToken> serializableTokens = new(data.Length);

		for (int i = 0; i < data.Length; i++)
		{
			byte datum = data[i];

			if (datum < EscapeCodes.Count)
			{
				if (datum == EscapeCodes.TokiSyllableString)
				{
					throw new NotImplementedException("toki syllable string decoding not implemented");
				}
				else if (datum == EscapeCodes.CapitalizedTokiSyllableString)
				{
					throw new NotImplementedException("capitalized toki syllable string decoding not implemented");
				}
				else if (datum == EscapeCodes.ASCIIString)
				{
					i++;
					List<byte> asciiBytes = [];
					for (; data[i] != 0x00; i++) // todo: could error on an invalid file (if file ends before null terminator)
					{ asciiBytes.Add(data[i]); }

					// return the null tokens to be 0x00
					for (int j = 0; j < asciiBytes.Count; j++)
					{ if (asciiBytes[j] == 0x80) { asciiBytes[j] = 0x00; } }

					if (asciiBytes.Count == 0)
					{
						serializableTokens.Add(new SpaceSupressor());
					}
					else
					{
						// todo: could error on an invalid file (give an informative error message)
						serializableTokens.AddRange(
							asciiEncoding.GetString(asciiBytes.ToArray())
								.Select(c => new CharToken(c))
							);
					}

				}
				else if (datum == EscapeCodes.UTF16String)
				{
					throw new NotImplementedException("UTF-16 string decoding not implemented");
				}
				else
				{
					throw new InvalidOperationException("unknown escape code");
				}
			}
			else if (datum < EscapeCodes.Count + punctuation.Length)
			{
				serializableTokens.Add(new PunctuationToken(datum - EscapeCodes.Count));
			}
			else
			{
				// todo: add safety to ensure that the index is not beyond the end of the word array
				serializableTokens.Add(new WordToken(datum - EscapeCodes.Count - punctuation.Length));
			}
		}

		return serializableTokens.ToArray();
	}
}