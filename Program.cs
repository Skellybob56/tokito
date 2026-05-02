
using System.Diagnostics;

namespace Tokito;

internal static class Program
{
	static void Main()
	{
		string textPath = "nasin_lete.txt";
		
		string text = File.ReadAllText(textPath);

		
		byte[] tokens = TokiCodex.Tokenize(text);
		byte[] compressed = ByteCodex.Compress(tokens, TokiCodex.minimumPairIndex);

		File.WriteAllBytes($"{textPath}.toki", compressed);

		Console.WriteLine(TokiCodex.Detokenize(ByteCodex.Decompress(compressed, TokiCodex.minimumPairIndex)));

		Console.Read(); // pause until enter
	}
}