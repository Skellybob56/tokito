
using System.Diagnostics;
using Tokito.Backend;

namespace Tokito.Frontend;

internal static class Program
{
	static void Main()
	{
		// todo: get from user input
		string textPath = "utf16_test.txt";
		bool useCRLF = true;
		
		string text = File.ReadAllText("texts\\" + textPath);

		// todo: future - remove debugging and messy code
		byte[] tokens = TokiCodex.Encode(text);

		File.WriteAllBytes($"tokis\\" + textPath + ".toki", tokens);

		Console.WriteLine("Written!");

		string decoded = TokiCodex.Decode(tokens, useCRLF);
		if (decoded == text) { Console.WriteLine("Decode success!"); }
		else { Console.WriteLine("Decode failure."); Debug.Fail("Untokenized tokens should be equivalent to the original text"); }

		Console.Read(); // pause until enter
	}
}
