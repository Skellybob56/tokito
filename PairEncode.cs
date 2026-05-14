
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
			LinkedList<TaggedByte> taggedTokens = [];

			// warning - huge arrays
			uint[] tokenPairFrequency = new uint[256*256];
			uint[] syllablePairFrequency = new uint[256*256];
			uint[] asciiPairFrequency = new uint[256*256];

			EscapeTag currentEscapeTag = EscapeTag.Token;
			for (int i = 0; i < serializedTokens.Count; i++)
			{
				// load current datum
				byte datum = serializedTokens[i];

				// update pair frequency
				if (i != 0) // not the first item
				{
					(EscapeTag previousEscapeTag, byte previousDatum) = taggedTokens.Last!.Value; // not the first item
					BytePair bytePair = new(previousDatum, datum);

					if (previousEscapeTag == EscapeTag.Token)
					{
						tokenPairFrequency[bytePair.ToIndex()]++;
					}
					else if (previousEscapeTag == EscapeTag.SyllableString)
					{
						syllablePairFrequency[bytePair.ToIndex()]++;
					}
					else if (previousEscapeTag == EscapeTag.AsciiString)
					{
						asciiPairFrequency[bytePair.ToIndex()]++;
					}
					// EscapeTag.Utf16String is intentionally skipped
				}

				// store current datum
				taggedTokens.AddLast(new TaggedByte(currentEscapeTag, datum));

				// update currentEscapeTag
				if (currentEscapeTag == EscapeTag.Token)
				{
					if (datum == EscapeCodes.TokiSyllableString || datum == EscapeCodes.CapitalizedTokiSyllableString)
					{
						currentEscapeTag = EscapeTag.SyllableString;
					}
					else if (datum == EscapeCodes.AsciiString)
					{
						currentEscapeTag = EscapeTag.AsciiString;
					}
					else if (datum == EscapeCodes.Utf16String)
					{
						currentEscapeTag = EscapeTag.Utf16String;
					}
				}
				else if (datum == 0x00) // string end
				{
					currentEscapeTag = EscapeTag.Token;
				}
			}

			return new FirstPassData(taggedTokens, tokenPairFrequency, syllablePairFrequency, asciiPairFrequency);
		}

		static (TaggedBytePair, uint frequency) MostFrequentPair(
			ReadOnlyCollection<uint>? tokenPairFrequency,
			ReadOnlyCollection<uint>? syllablePairFrequency,
			ReadOnlyCollection<uint>? asciiPairFrequency)
		{
			TaggedBytePair mostFrequentPair = new(EscapeTag.Token, new(0, 0));
			uint greatestFrequency = 0;

			// todo: reduce repetition
			if (tokenPairFrequency is not null)
			{
				for (int i = 0; i < tokenPairFrequency.Count; i++)
				{
					uint frequency = tokenPairFrequency[i];
					if (frequency > greatestFrequency)
					{
						mostFrequentPair = new(EscapeTag.Token, BytePair.FromIndex(i));
						greatestFrequency = frequency;
					}
				}
			}

			if (syllablePairFrequency is not null)
			{
				for (int i = 0; i < syllablePairFrequency.Count; i++)
				{
					uint frequency = syllablePairFrequency[i];
					if (frequency > greatestFrequency)
					{
						mostFrequentPair = new(EscapeTag.SyllableString, BytePair.FromIndex(i));
						greatestFrequency = frequency;
					}
				}
			}

			if (asciiPairFrequency is not null)
			{
				for (int i = 0; i < asciiPairFrequency.Count; i++)
				{
					uint frequency = asciiPairFrequency[i];
					if (frequency > greatestFrequency)
					{
						mostFrequentPair = new(EscapeTag.AsciiString, BytePair.FromIndex(i));
						greatestFrequency = frequency;
					}
				}
			}

			return (mostFrequentPair, greatestFrequency);
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