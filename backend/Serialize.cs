
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
			bytes.AddRange(EncodeString(consecutiveChars.ToString()));
			consecutiveChars.Clear();
		}

		return bytes.ToArray();

		static byte[] EncodeString(string word)
		{
			return Ascii.IsValid(word)? EncodeAsciiString(word) : EncodeUtf8String(word);
		}

		static byte[] EncodeAsciiString(string word)
		{
			byte[] asciiString = new byte[1 + word.Length + 1];
			asciiString[0] = EscapeCodes.AsciiString;
			asciiEncodingStrict.GetBytes(word, asciiString.AsSpan(1)); // paste the string bytes in after the escape code
			asciiString[^1] = 0x00; // todo: perhaps tidy this null terminator into a constant somewhere

			// replace nulls in the text with an explicit null token
			for (int i = 1; i < asciiString.Length - 1; i++)
			{ if (asciiString[i] == 0x00) { asciiString[i] = 0x80; } }

			return asciiString;
		}

		static byte[] EncodeUtf8String(string word)
		{
			// todo: this will break down with embedded nulls. create a system that allows embedded nulls to be automatically encoded as ascii 0x80
			int dataByteCount = utf8EncodingStrict.GetByteCount(word);
			byte[] utf8String = new byte[1 + dataByteCount + 1];
			utf8String[0] = EscapeCodes.Utf8String;
			utf8EncodingStrict.GetBytes(word, utf8String.AsSpan(1)); // paste the string bytes in after the escape code
			utf8String[^1] = 0x00; // todo: perhaps tidy this null terminator into a constant somewhere

			return utf8String;
		}
	}
}
