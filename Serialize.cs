
using System.Buffers.Binary;

namespace Tokito;

static partial class TokiCodex
{
	static byte[] Serialize(LogicalToken[] tokens)
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
		
		throw new NotImplementedException();
	}
}
