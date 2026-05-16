
namespace Tokito.Backend;

static partial class TokiCodex
{
	enum EscapeTag : byte
	{
		Token,
		SyllableString,
		AsciiString,
		Utf8String
	}

	readonly record struct TaggedByte(EscapeTag Tag, byte Value);

	readonly record struct BytePair(byte Pair1, byte Pair2)
	{
		public int ToIndex()
		{
			return Pair1 * 256 + Pair2;
		}

		public static int ToIndex(byte Pair1, byte Pair2)
		{
			return Pair1 * 256 + Pair2;
		}

		public static BytePair FromIndex(int index)
		{
			return new((byte)(index >> 8), (byte)index);
		}
	}

	readonly record struct TaggedBytePair(EscapeTag Tag, BytePair Pair);
}
