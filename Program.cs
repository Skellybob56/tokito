
using System.Diagnostics;

namespace Tokito;

internal static class Program
{
	// todo: make a BytePair struct to simplify syntax - replace `(byte p1, byte p2)`
	static (byte[] tokens, (byte p1, byte p2)[] pairs) PairEncode(byte[] tokens, byte minimumPairIndex)
	{
		if (minimumPairIndex == 0) { throw new ArgumentOutOfRangeException(nameof(minimumPairIndex), "Cannot be zero"); }

		byte maximumPairCount = (byte)(256 - minimumPairIndex);

		List<(byte p1, byte p2)> pairs = [];
		LinkedList<byte> linkedTokens = new(tokens); // perf: this could be replaced with a byte?[] array where removed bytes are just nulled - would improve caching

		{
			int[] pairFrequency = new int[256 * 256]; // warning - large array

			// find the frequency of all adjecent token pairs
			for (LinkedListNode<byte>? node = linkedTokens.First; node is not null && node.Next is not null; node = node.Next)
			{
				int index = node.Value * 256 + node.Next.Value;
				pairFrequency[index]++;
			}

			while (pairs.Count < maximumPairCount)
			{
				// find the most common adjacent token pair
				int mostCommonPairIndex = 0;
				int greatestFrequency = pairFrequency[0];
				for (int i = 1; i < pairFrequency.Length; i++) // perf: this could avoid iterating over the sections known to be empty
				{
					if (greatestFrequency < pairFrequency[i])
					{
						mostCommonPairIndex = i;
						greatestFrequency = pairFrequency[i];
					}
				}

				// if the most common pair's frequency is less than three: break the loop (too infrequent for useful compression)
				if (greatestFrequency < 3) { break; }

				// add the pair to a pair list
				(byte p1, byte p2) mostCommonPair = ((byte)(mostCommonPairIndex >> 8), (byte)mostCommonPairIndex);
				byte pairToken = (byte)(minimumPairIndex + pairs.Count);
				pairs.Add(mostCommonPair);

				// replace all instances of the pair in the tokens array and do a partial update of the frequency table for pairs with relation to one of the bytes that have been updated
				for (LinkedListNode<byte>? node = linkedTokens.First; node is not null && node.Next is not null; node = node.Next)
				{
					if (node.Value == mostCommonPair.p1 && node.Next.Value == mostCommonPair.p2) // pair hit
					{
						// update pairFrequency
						if (node.Previous is not null)
						{
							pairFrequency[node.Previous.Value * 256 + node.Value]--;
							pairFrequency[node.Previous.Value * 256 + pairToken]++;
						}
						if (node.Next.Next is not null)
						{
							pairFrequency[node.Next.Value * 256 + node.Next.Next.Value]--;
							pairFrequency[pairToken * 256 + node.Next.Next.Value]++;
						}
						
						// update linkedTokens with the pair compresison
						node.Value = pairToken;
						linkedTokens.Remove(node.Next); // after this, node.Next may be null
					}
				}
				// zero the frequency of the replaced pair
				pairFrequency[mostCommonPair.p1 * 256 + mostCommonPair.p2] = 0;
			}
		}

		return (linkedTokens.ToArray(), pairs.ToArray());
	}

	static byte[] GenerateHeader((byte p1, byte p2)[] pairs)
	{
		if (pairs.Length > byte.MaxValue)
		{ throw new ArgumentException("The current format does not allow for more than 255 pairs", nameof(pairs)); }
		byte[] header = new byte[1 + 2*pairs.Length];
		header[0] = (byte)pairs.Length;

		for (int i = 0; i < pairs.Length; i++)
		{
			header[1 + 2*i] = pairs[i].p1;
			header[2 + 2*i] = pairs[i].p2;
		}

		return header;
	}

	static byte[] Compress(byte[] tokens, byte? minimumPairIndex)
	{
		if (minimumPairIndex is not null)
		{
			(tokens, (byte p1, byte p2)[] pairs) = PairEncode(tokens, minimumPairIndex.Value);
			byte[] header = GenerateHeader(pairs);

			byte[] compressed = new byte[header.Length + tokens.Length];
			header.CopyTo(compressed, 0);
			tokens.CopyTo(compressed, header.Length);

			return compressed;
		}
		else
		{
			byte[] compressed = new byte[tokens.Length + 1];
			compressed[0] = 0;
			tokens.CopyTo(compressed, 1);

			return compressed;
		}
	}

	static byte[] Decompress(byte[] compressed, byte? minimumPairIndex)
	{
		// if no pairs, just remove the initial 'zero pairs' byte and return
		if (minimumPairIndex is null) { return compressed[1..]; }

		// unpack pairs array
		(byte p1, byte p2)[] pairs = new (byte p1, byte p2)[compressed[0]];
		for (int i = 0; i < pairs.Length; i++)
		{ pairs[i] = (compressed[2*i + 1], compressed[2 * i + 2]); }

		// todo: check if any pairs are invalid (e.g. circularity)

		LinkedList<byte> linkedTokens = new(compressed[(1 + pairs.Length*2)..]); // perf: instead of a linked list, we could check the depth of all pairs and precalcualte final array size and then decompress into this final array - would improve caching

		for (LinkedListNode<byte>? node = linkedTokens.First; node is not null; )
		{
			if (node.Value >= minimumPairIndex)
			{
				(byte p1, byte p2) pair = pairs[node.Value - minimumPairIndex.Value];
				node.Value = pair.p1;
				linkedTokens.AddAfter(node, pair.p2);
			}
			else { node = node.Next; } // only progress when not spliting a pair
		}

		return linkedTokens.ToArray();
	}

	static void Main()
	{
		string textPath = "nasin_lete.txt";
		
		string text = File.ReadAllText(textPath);

		
		byte[] tokens = TokiCodex.Tokenize(text);
		byte[] compressed = Compress(tokens, TokiCodex.minimumPairIndex);

		File.WriteAllBytes($"{textPath}.toki", compressed);

		Console.WriteLine(TokiCodex.Detokenize(Decompress(compressed, TokiCodex.minimumPairIndex)));

		Console.Read(); // pause until enter
	}
}