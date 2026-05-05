
using System.Buffers.Binary;
using System.Text;

namespace Tokito;

static partial class TokiCodex
{
	static byte[] Serialize(SerializableToken[] tokens)
	{
		static byte[] EncodeUTF8String(string word)
		{
			int dataByteCount = strictUTF8Encoding.GetByteCount(word);

			int neededByteDepth;
			if (dataByteCount < byte.MaxValue) // exclusive to allow for max value sentinel
			{ neededByteDepth = 1; }
			else if (dataByteCount < ushort.MaxValue)
			{ neededByteDepth = 2; }
			else { neededByteDepth = 4; }

			int dataStartIndex = 1 + (2 * neededByteDepth - 1);

			byte[] utf8String = new byte[dataStartIndex + dataByteCount];
			utf8String[0] = EscapeCodes.UTF8String;
			
			// write any needed sentinels
			for (int i = 1; i < neededByteDepth; i++)
			{ utf8String[i] = 0xFF; }

			// write length token
			if(neededByteDepth == 1)
			{ utf8String[neededByteDepth] = (byte)dataByteCount; }
			else if (neededByteDepth == 2)
			{ BinaryPrimitives.WriteUInt16LittleEndian(utf8String.AsSpan(neededByteDepth), (ushort)dataByteCount); }
			else if (neededByteDepth == 4)
			{ BinaryPrimitives.WriteUInt32LittleEndian(utf8String.AsSpan(neededByteDepth), (uint)dataByteCount); }

			strictUTF8Encoding.GetBytes(word, utf8String.AsSpan(dataStartIndex)); // paste the string bytes in at the dataStartIndex

			return utf8String;
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
					bytes.AddRange(EncodeUTF8String(consecutiveChars.ToString()));
					consecutiveChars.Clear();
				}

				if (token is WordToken wordToken)
				{ bytes.Add((byte)(EscapeCodes.Count + punctuation.Length + wordToken.WordIndex)); }
				else if (token is PunctuationToken punctuationToken)
				{ bytes.Add((byte)(EscapeCodes.Count + punctuationToken.PunctuationIndex)); }
				else if (token is SpaceSupressor)
				{
					// zero length UTF-8 string suppresses automatic spacing
					bytes.Add(EscapeCodes.UTF8String);
					bytes.Add(0x00);
				}
			}
		}
		if (consecutiveChars.Length != 0)
		{
			// save array
			// todo: implement UTF-16 support
			bytes.AddRange(EncodeUTF8String(consecutiveChars.ToString()));
			consecutiveChars.Clear();
		}

		return bytes.ToArray();
	}
}
