
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

		File.WriteAllBytes($"{textPath}.toki", tokens);

		Console.WriteLine("Written!");

		string decoded = TokiCodex.Decode(tokens, useCRLF);
		if (decoded == text) { Console.WriteLine("Decode success!"); }
		else { Console.WriteLine("Decode failure."); Debug.Fail("Untokenized tokens should be equivalent to the original text"); }

		Console.Read(); // pause until enter
	}
}