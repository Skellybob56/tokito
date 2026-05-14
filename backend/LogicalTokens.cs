
namespace Tokito.Backend;

static partial class TokiCodex
{
	abstract record LogicalToken
	{
		// if automatically spaces itself from a following word
		public bool Spaced() => this switch
		{
			WordToken => true,
			PunctuationToken pt => punctuation[pt.PunctuationIndex].spaced,
			_ => false
		};

		// this will become useful as a toki pona syllable string will also be considered a word here
		public bool Word() => this switch
		{
			WordToken => true,
			_ => false
		};
	}
	record ExplicitSpaceToken : LogicalToken;

	abstract record SerializableToken : LogicalToken;
	record WordToken(int WordIndex) : SerializableToken;
	record PunctuationToken(int PunctuationIndex) : SerializableToken;
	record CharToken(char Value) : SerializableToken;
	record SpaceSupressor : SerializableToken;

	// todo: add token(s) for toki pona syllables
}
