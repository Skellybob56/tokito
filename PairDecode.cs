
using System.Collections.ObjectModel;

namespace Tokito;

static partial class TokiCodex
{
	readonly record struct PairCollection(
		BytePair[] TokenPairs,
		BytePair[] SyllablePairs,
		BytePair[] AsciiPairs
	);

	static byte[] PairDecode(byte[] compressedBytes)
	{
		// todo: this is a duplicate of the code at the start of PairEncode()
		byte? minTokenPairIndex = tokenCount < 256 ? (byte)tokenCount : null;
		const byte minSyllablePairIndex = 103;
		const byte minAsciiPairIndex = 0x81;

		(PairCollection pairCollection, int headerSize) = ReadHeader(compressedBytes.AsReadOnly());

		LinkedList<byte> serializedTokens = new(compressedBytes[headerSize..]);

		EscapeTag currentEscapeTag = EscapeTag.Token;
		for (LinkedListNode<byte>? node = serializedTokens.First; node is not null; )
		{
			// if is pair then split the pair, otherwise move on
			// todo: reduce repetition
			if (currentEscapeTag == EscapeTag.Token && node.Value >= minTokenPairIndex)
			{
				BytePair pair = pairCollection.TokenPairs[node.Value - minTokenPairIndex.Value];
				node.Value = pair.Pair1;
				serializedTokens.AddAfter(node, pair.Pair2);
				continue;
			}
			else if (currentEscapeTag == EscapeTag.SyllableString && node.Value >= minSyllablePairIndex)
			{
				BytePair pair = pairCollection.SyllablePairs[node.Value - minSyllablePairIndex];
				node.Value = pair.Pair1;
				serializedTokens.AddAfter(node, pair.Pair2);
				continue;
			}
			else if (currentEscapeTag == EscapeTag.AsciiString && node.Value >= minAsciiPairIndex)
			{
				BytePair pair = pairCollection.AsciiPairs[node.Value - minAsciiPairIndex];
				node.Value = pair.Pair1;
				serializedTokens.AddAfter(node, pair.Pair2);
				continue;
			}
			else // not a pair
			{
				// update currentEscapeTag
				if (currentEscapeTag == EscapeTag.Token)
				{
					if (node.Value == EscapeCodes.TokiSyllableString || node.Value == EscapeCodes.CapitalizedTokiSyllableString)
					{
						currentEscapeTag = EscapeTag.SyllableString;
					}
					else if (node.Value == EscapeCodes.AsciiString)
					{
						currentEscapeTag = EscapeTag.AsciiString;
					}
					else if (node.Value == EscapeCodes.Utf16String)
					{
						currentEscapeTag = EscapeTag.Utf16String;
					}
				}
				else if (node.Value == 0x00) // string end
				{
					currentEscapeTag = EscapeTag.Token;
				}

				node = node.Next;
			}
		}

		return serializedTokens.ToArray();

		static (PairCollection pairCollection, int headerSize) ReadHeader(ReadOnlyCollection<byte> compressedBytes)
		{
			BytePair[][] bytePairs = new BytePair[3][];
			int pointer = 0;
			for (int section = 0; section < bytePairs.Length; section++)
			{
				// load size
				byte size = compressedBytes[pointer];
				BytePair[] pairSet = new BytePair[size];

				pointer++;
				for (int pairIndex = 0; pairIndex < size; pairIndex++)
				{
					byte pair1 = compressedBytes[pointer]; pointer++;
					byte pair2 = compressedBytes[pointer]; pointer++;
					pairSet[pairIndex] = new(pair1, pair2);
				}
				bytePairs[section] = pairSet;
			}

			return (new(bytePairs[0], bytePairs[1], bytePairs[2]), pointer);
		}
	}
}
