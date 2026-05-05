
using System.Diagnostics;

namespace Tokito;

internal static class Program
{
	static void Main()
	{
		// todo: get from user input
		string textPath = "akesi_laso_en_jan_utala_lipu_nanpa_wan.txt";
		bool useCRLF = true;
		
		string text = File.ReadAllText(textPath);

		// todo: future - remove debugging and messy code
		byte[] tokens = TokiCodex.Encode(text);
		byte[] compressed = ByteCodex.Compress((byte[])tokens.Clone(), TokiCodex.minimumPairIndex);

		File.WriteAllBytes($"{textPath}.toki", compressed);

		Console.WriteLine("Done!");

		byte[] decompressed = ByteCodex.Decompress(compressed, TokiCodex.minimumPairIndex);
		Debug.Assert(decompressed.SequenceEqual(tokens), "Decompressed data should be equivalent to the original tokens");
		string decoded = TokiCodex.Decode(tokens, useCRLF);
		Debug.Assert(decoded == text, "Untokenized tokens should be equivalent to the original text");

		Console.Read(); // pause until enter
	}
}