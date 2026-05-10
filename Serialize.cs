
using System.Text;

namespace Tokito;

static partial class TokiCodex
{
	static byte[] Serialize(SerializableToken[] tokens)
	{
		static byte[] EncodeAsciiString(string word)
		{
			int dataByteCount = asciiEncoding.GetByteCount(word);

			byte[] asciiString = new byte[1 + dataByteCount + 1];
			asciiString[0] = EscapeCodes.ASCIIString;
			asciiEncoding.GetBytes(word, asciiString.AsSpan(1)); // paste the string bytes in after the escape code
			asciiString[^1] = 0x00; // todo: tidy this null terminator into a constant somewhere

			// replace nulls in the text with an explicit null token
			for (int i = 1; i < asciiString.Length - 1; i++)
			{ if (asciiString[i] == 0x00) { asciiString[i] = 0x80; } }

			return asciiString;
		}
		
		List<byte> bytes = new(tokens.Length);

		StringBuilder consecutiveChars = new();
		foreach (SerializableToken token in tokens)
		{
			if (token is CharToken charToken)
			{
				consecutiveChars.Append(charToken.Value);
			}
			else
			{
				if (consecutiveChars.Length != 0)
				{
					// save array
					// todo: implement UTF-16 support
					bytes.AddRange(EncodeAsciiString(consecutiveChars.ToString()));
					consecutiveChars.Clear();
				}

				if (token is WordToken wordToken)
				{ bytes.Add((byte)(EscapeCodes.Count + punctuation.Length + wordToken.WordIndex)); }
				else if (token is PunctuationToken punctuationToken)
				{ bytes.Add((byte)(EscapeCodes.Count + punctuationToken.PunctuationIndex)); }
				else if (token is SpaceSupressor)
				{
					// an immediately terminated ascii string suppresses automatic spacing
					bytes.Add(EscapeCodes.ASCIIString);
					bytes.Add(0x00);
				}
			}
		}
		if (consecutiveChars.Length != 0)
		{
			// save array
			// todo: implement UTF-16 support
			bytes.AddRange(EncodeAsciiString(consecutiveChars.ToString()));
			consecutiveChars.Clear();
		}

		return bytes.ToArray();
	}
}
