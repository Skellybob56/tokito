
using System.Text;

namespace Tokito;

static partial class TokiCodex
{
	abstract record LogicalToken;
	record WordToken(int WordIndex) : LogicalToken;
	record PunctuationToken(int PunctuationIndex) : LogicalToken;
	record CharToken(char Value) : LogicalToken;
	
	// todo: add token(s) for toki pona syllables
}
