
using System.Collections.Immutable;
using System.Text;

namespace Tokito.Backend;

static partial class TokiCodex
{
	static readonly Encoding strictAsciiEncoding = Encoding.GetEncoding(
		"us-ascii", // ascii encoding that crashes when encoding/decoding non-ascii bytes
		new EncoderExceptionFallback(), 
		new DecoderExceptionFallback()
	);
	static readonly UnicodeEncoding strictUTF16Encoding = new(false, false, true); // use little endian, do not prepend BOM, do throw on invalid bytes

	static class EscapeCodes
	{
		public const int Count = 6; // must be manually updated
		public const byte UpdatePairData = 0x00;
		public const byte InsertSpace = 0x01;
		public const byte TokiSyllableString = 0x02;
		public const byte CapitalizedTokiSyllableString = 0x03;
		public const byte AsciiString = 0x04;
		public const byte Utf16String = 0x05;
	}

	// todo: load these from data files
	// todo: consider adding '-' along with pre-spacing capability to allow it's spacing to work
	readonly record struct SpaceableChar(char Character, bool Spaced)
	{
		public static implicit operator SpaceableChar((char Character, bool Spaced) origin)
		{ return new SpaceableChar(origin.Character, origin.Spaced); }
	}
	static readonly ImmutableArray<SpaceableChar> punctuation = [('\n', false), ('.', true), (',', true), (':', true), ('"', false), ('?', true), ('!', true), ('\'', false)];
	static readonly string[] words = ["a", "akesi", "ala", "alasa", "ale", "anpa", "ante", "anu", "awen", "e", "en", "esun", "ijo", "ike", "ilo", "insa", "jaki", "jan", "jelo", "jo", "kala", "kalama", "kama", "kasi", "ken", "kepeken", "kili", "kin", "kiwen", "ko", "kon", "kule", "kulupu", "kute", "la", "lape", "laso", "lawa", "len", "lete", "li", "lili", "linja", "lipu", "loje", "lon", "luka", "lukin", "lupa", "ma", "mama", "mani", "mi", "moku", "moli", "monsi", "monsuta", "mu", "mun", "musi", "mute", "nanpa", "nasa", "nasin", "nena", "ni", "nimi", "noka", "o", "olin", "ona", "open", "pakala", "pali", "palisa", "pan", "pana", "pi", "pilin", "pimeja", "pini", "pipi", "poka", "poki", "pona", "sama", "seli", "selo", "seme", "sewi", "sijelo", "sike", "sin", "sina", "sinpin", "sitelen", "sona", "soweli", "suli", "suno", "supa", "suwi", "tan", "taso", "tawa", "telo", "tenpo", "toki", "tomo", "tu", "unpa", "uta", "utala", "walo", "wan", "waso", "wawa", "weka", "wile"];
	
	static readonly int tokenCount = EscapeCodes.Count + punctuation.Length + words.Length;
	static readonly byte? minTokenPairIndex = tokenCount < 256? (byte)tokenCount : null;
	static readonly int tokenPairSlots = 256 - tokenCount;
	const byte minSyllablePairIndex = 103;
	const int syllablePairSlots = 256 - minSyllablePairIndex;
	const byte minAsciiPairIndex = 0x81;
	const int asciiPairSlots = 256 - minAsciiPairIndex;

	static TokiCodex()
	{
		if (tokenCount > 256)
		{ throw new ArgumentException($"The current format does not allow for more than 256 total escape codes, punctuation and words", nameof(words) + ", " + nameof(punctuation)); }

	}

	public static byte[] Encode(string text)
	{
		return PairEncode(Serialize(Tokenize(text)));
	}

	public static string Decode(byte[] encoded, bool useCRLF)
	{
		return Detokenize(Deserialize(PairDecode(encoded)), useCRLF);
	}
}
