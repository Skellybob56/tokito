
using System.Text;

namespace Tokito.Backend;

static partial class TokiCodex
{
	static byte[] Serialize(SerializableToken[] tokens)
	{
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
					bytes.AddRange(EncodeString(consecutiveChars.ToString()));
					consecutiveChars.Clear();
				}

				if (token is WordToken wordToken)
				{ bytes.Add((byte)(EscapeCodes.Count + punctuation.Length + wordToken.WordIndex)); }
				else if (token is PunctuationToken punctuationToken)
				{ bytes.Add((byte)(EscapeCodes.Count + punctuationToken.PunctuationIndex)); }
				else if (token is SpaceSupressor)
				{
					// an immediately terminated ascii string suppresses automatic spacing
					bytes.Add(EscapeCodes.AsciiString);
					bytes.Add(0x00);
				}
			}
		}
		if (consecutiveChars.Length != 0)
		{
			// save array
			// todo: implement UTF-16 support
			bytes.AddRange(EncodeString(consecutiveChars.ToString()));
			consecutiveChars.Clear();
		}

		return bytes.ToArray();

		static byte[] EncodeString(string word)
		{
			return Ascii.IsValid(word)? EncodeAsciiString(word) : EncodeUtf16String(word);
		}

		static byte[] EncodeAsciiString(string word)
		{
			byte[] asciiString = new byte[1 + word.Length + 1];
			asciiString[0] = EscapeCodes.AsciiString;
			strictAsciiEncoding.GetBytes(word, asciiString.AsSpan(1)); // paste the string bytes in after the escape code
			asciiString[^1] = 0x00; // todo: tidy this null terminator into a constant somewhere

			// replace nulls in the text with an explicit null token
			for (int i = 1; i < asciiString.Length - 1; i++)
			{ if (asciiString[i] == 0x00) { asciiString[i] = 0x80; } }

			return asciiString;
		}

		static byte[] EncodeUtf16String(string word)
		{
			throw new NotImplementedException();
		}
	}
}
