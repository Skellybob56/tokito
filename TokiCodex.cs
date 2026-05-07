
using System.Text;

namespace Tokito;

static partial class TokiCodex
{
	static readonly UTF8Encoding strictUTF8Encoding = new(false, true); // do not prepend BOM, do throw on invalid bytes

	static class EscapeCodes
	{
		public const int Count = 6; // must be manually updated
		public const byte UpdatePairData = 0x00;
		public const byte InsertSpace = 0x01;
		public const byte TokiSyllableString = 0x02;
		public const byte CapitalizedTokiSyllableString = 0x03;
		public const byte UTF8String = 0x04;
		public const byte UTF16String = 0x05;
	}

	// todo: load these from data files
	// todo: consider adding '-' along with pre-spacing capability to allow it's spacing to work
	// todo: make SpaceableChar struct to improve readability
	static readonly (char character, bool spaced)[] punctuation = [('\n', false), ('.', true), (',', true), (':', true), ('"', false), ('?', true), ('!', true), ('\'', false)];
	static readonly string[] words = ["a", "akesi", "ala", "alasa", "ale", "anpa", "ante", "anu", "awen", "e", "en", "esun", "ijo", "ike", "ilo", "insa", "jaki", "jan", "jelo", "jo", "kala", "kalama", "kama", "kasi", "ken", "kepeken", "kili", "kin", "kiwen", "ko", "kon", "kule", "kulupu", "kute", "la", "lape", "laso", "lawa", "len", "lete", "li", "lili", "linja", "lipu", "loje", "lon", "luka", "lukin", "lupa", "ma", "mama", "mani", "mi", "moku", "moli", "monsi", "monsuta", "mu", "mun", "musi", "mute", "nanpa", "nasa", "nasin", "nena", "ni", "nimi", "noka", "o", "olin", "ona", "open", "pakala", "pali", "palisa", "pan", "pana", "pi", "pilin", "pimeja", "pini", "pipi", "poka", "poki", "pona", "sama", "seli", "selo", "seme", "sewi", "sijelo", "sike", "sin", "sina", "sinpin", "sitelen", "sona", "soweli", "suli", "suno", "supa", "suwi", "tan", "taso", "tawa", "telo", "tenpo", "toki", "tomo", "tu", "unpa", "uta", "utala", "walo", "wan", "waso", "wawa", "weka", "wile"];
	
	static readonly int tokenCount = EscapeCodes.Count + punctuation.Length + words.Length; // todo: add safety that ensures that this is <= 256
	public static readonly byte? minimumPairIndex = tokenCount < 256? (byte)tokenCount : null;

	static TokiCodex()
	{
		if (EscapeCodes.Count + punctuation.Length + words.Length > byte.MaxValue + 1)
		{ throw new ArgumentException($"The current format does not allow for more than {byte.MaxValue + 1} total escape codes, punctuation and words", nameof(words) + ", " + nameof(punctuation)); }

	}

	public static byte[] Encode(string text)
	{
		return Serialize(Tokenize(text));
	}

	public static string Decode(byte[] encoded, bool useCRLF)
	{
		return Detokenize(Deserialize(encoded), useCRLF);
	}
}
