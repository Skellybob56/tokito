
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Tokito;

static partial class TokiCodex
{
	enum EscapeTag : byte
	{
		Token,
		SyllableString,
		AsciiString,
		Utf16String
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

	readonly record struct FirstPassData(
		LinkedList<TaggedByte> TaggedTokens,
		uint[] TokenPairFrequency,
		uint[] SyllablePairFrequency,
		uint[] AsciiPairFrequency
	);

	static byte[] PairEncode(byte[] serializedTokens)
	{
		// todo: consider doing this in a more organised manner
		byte minTokenPairIndex = (byte)tokenCount; // potential wrap doesn't matter because tokenPairSlots will be correct (and less than one if a wrap occurs)
		int tokenPairSlots = 256 - tokenCount;
		const byte minSyllablePairIndex = 103;
		const int syllablePairSlots = 256 - minSyllablePairIndex;
		const byte minAsciiPairIndex = 0x81;
		const int asciiPairSlots = 256 - minAsciiPairIndex;

		// gather initial pair frequency info and taggedTokens
		(LinkedList<TaggedByte> taggedTokens,
			uint[] tokenPairFrequency,
			uint[] syllablePairFrequency,
			uint[] asciiPairFrequency) =
			TagTokensAndCountPairFrequency(serializedTokens.AsReadOnly());

		List<BytePair> tokenPairs = [];
		bool tokenPairsFull = tokenPairSlots <= 0;
		List<BytePair> syllablePairs = [];
		bool syllablePairsFull = syllablePairSlots <= 0;
		List<BytePair> asciiPairs = [];
		bool asciiPairsFull = asciiPairSlots <= 0;

		while (true)
		{
			// find most frequent pair (in only the pair sections that have remaining pair slots)
			(TaggedBytePair mostFrequentPair, uint frequency) = MostFrequentPair(
				tokenPairsFull? null : tokenPairFrequency.AsReadOnly(),
				syllablePairsFull? null : syllablePairFrequency.AsReadOnly(),
				asciiPairsFull? null : asciiPairFrequency.AsReadOnly()
				);

			// break the most frequent pair is too rare
			if (frequency < 3) { break; }

			// add pair to pairs array
			byte pairIndex;
			if (mostFrequentPair.Tag == EscapeTag.Token)
			{
				pairIndex = (byte)(minTokenPairIndex + tokenPairs.Count); // should be safe
				tokenPairs.Add(mostFrequentPair.Pair);
				tokenPairsFull = tokenPairs.Count >= tokenPairSlots;
			}
			else if (mostFrequentPair.Tag == EscapeTag.SyllableString)
			{
				pairIndex = (byte)(minSyllablePairIndex + syllablePairs.Count); // should be safe
				syllablePairs.Add(mostFrequentPair.Pair);
				syllablePairsFull = syllablePairs.Count >= syllablePairSlots;
			}
			else if (mostFrequentPair.Tag == EscapeTag.AsciiString)
			{
				pairIndex = (byte)(minAsciiPairIndex + asciiPairs.Count); // should be safe
				asciiPairs.Add(mostFrequentPair.Pair);
				asciiPairsFull = asciiPairs.Count >= asciiPairSlots;
			}
			else // EscapeTag.Utf16String or another unknown EscapeTag
			{
				throw new UnreachableException("The most frequent pair should be in a compressible escape string");
			}

			// replace all instances of the pair in taggedTokens and update pair frequency arrays to reflect the mutation
			ReplaceTaggedPairAndUpdateFrequencies(pairIndex, mostFrequentPair, ref taggedTokens,
				ref tokenPairFrequency, ref syllablePairFrequency, ref asciiPairFrequency);

			// break if there are no pair spaces left at all
			if (tokenPairsFull && syllablePairsFull && asciiPairsFull) { break; }
		}

		// remove tags from taggedTokens and add the header
		byte[] compressedBytes = DistillAndHeadTaggedTokens(taggedTokens, tokenPairs, syllablePairs, asciiPairs);

		return compressedBytes;

		static FirstPassData TagTokensAndCountPairFrequency(ReadOnlyCollection<byte> serializedTokens)
		{
			throw new NotImplementedException();
		}

		static (TaggedBytePair, uint frequency) MostFrequentPair(
			ReadOnlyCollection<uint>? tokenPairFrequency,
			ReadOnlyCollection<uint>? syllablePairFrequency,
			ReadOnlyCollection<uint>? asciiPairFrequency)
		{
			throw new NotImplementedException();
		}

		static void ReplaceTaggedPairAndUpdateFrequencies(byte pairIndex, TaggedBytePair pair, ref LinkedList<TaggedByte> taggedTokens,
			ref uint[] tokenPairFrequency, ref uint[] syllablePairFrequency, ref uint[] asciiPairFrequency)
		{
			throw new NotImplementedException();
		}

		static byte[] DistillAndHeadTaggedTokens(LinkedList<TaggedByte> taggedTokens,
			List<BytePair> tokenPairs, List<BytePair> syllablePairs, List<BytePair> asciiPairs)
		{
			throw new NotImplementedException();
		}
	}
}