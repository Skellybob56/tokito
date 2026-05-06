
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
				else if (datum == EscapeCodes.UTF8String)
				{
					// load length value
					i++;
					uint length = data[i];
					i++;
					if (length == byte.MaxValue)
					{
						length = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(i, 2));
						i += 2;
						if (length == ushort.MaxValue)
						{
							length = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(i, 4));
							i += 4;
						}
					}

					if (length == 0)
					{
						serializableTokens.Add(new SpaceSupressor());
						i--; // move back onto the last length byte
					}
					else
					{
						// todo: consider if it is a problem that this can only get up to an int.MaxValue length string - perhaps put this in the documentation
						// todo: could error on an invalid file (give an informative error message)
						serializableTokens.AddRange(
							strictUTF8Encoding.GetString(data, i, (int)length)
								.Select(c => new CharToken(c))
							);
						i += (int)length - 1; // move forward to the last item of the string, the for loop will move us onto the next token
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