
using System.Buffers.Binary;
using System.Text;

namespace Tokito;

static partial class TokiCodex
{
	// todo: add options for how lossy encoding should be
	// todo: add capability for encoding losslessly (implement escape codes)
	public static byte[] Tokenize(string text)
	{
		if (EscapeCodes.Count + punctuation.Length + words.Length > byte.MaxValue + 1)
		{ throw new ArgumentException($"The current format does not allow for more than {byte.MaxValue + 1} total escape codes, punctuation and words", nameof(words) + ", " + nameof(punctuation)); }

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

		static byte[] ParseWord(string word, int punctuationLength, string[] words)
		{
			// todo: throw if word is null or empty

			if (words.Contains(word))
			{ return [(byte) (EscapeCodes.Count + punctuationLength + words.IndexOf(word))]; }

			// on fail attempt to encode it as a UTF-8 string (as it is alphabetic, it must fit in UTF-8)
			return EncodeUTF8String(word);
		}

		List<byte> tokens = [];
		
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
					tokens.AddRange(ParseWord(currentWord, punctuation.Length, words));
					currentWord = "";
				}

				if (character == ' ')
				{
					// todo: check if this space is predicted: if not then add the missing space as a token
					// todo: also check if a space is predicted in an area where there should be no space and use some method to supress it. example: 'ona.mi'
				}
				else if (character == '\r') { } // silently ignores \r
				else if (punctuation.Any(p => p.character == character))
				{
					tokens.Add((byte)(EscapeCodes.Count + Array.FindIndex(punctuation, p => p.character == character)));
				}
				else
				{
					throw new NotImplementedException("unknown symbol"); // causes a feature regression but is more honest. this regression is needed for future lossless encoding
				}
			}
		}
		if (currentWord != "")
		{ tokens.AddRange(ParseWord(currentWord, punctuation.Length, words)); }

		return tokens.ToArray();
	}
}
