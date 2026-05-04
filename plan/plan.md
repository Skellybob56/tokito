# theory

## tokenisation
This will require a word dictionary and a hard-coded set of punctuation. The reason that the set is hard-coded is because many punctuation marks requrie special code and they are rather standard. It would be ideal to make the punctuation system data-driven. Possibly, in the future, I could add a byte that acts to slice the (frequency ordered) punctuation set down to a smaller set allowing potentially greater compression when encoding basic punctuation at the cost of an extra header byte.

The text will be assumed to be made only of punctuation, whitespace and letters. Any instance of an uppercase letter will be assumed to be the start of a proper noun. For now, proper nouns will be encoded simply in ascii but in the future, I hope that I will be able to detect when I can incode these as toki pona syllables which will allow for greater compression. ASCII will be encoded by having a specific internal punctuation that means "start ascii string" and by using the ASCII end of string character to end it.
If a document written using only these characters is encoded, it will be decoded identically to the original with merely two exceptions. Capital letters that aren't at the start of words will be removed. Secondly, double spaces or spaces in other strange locations, e.g. before a comma, will be removed as spaces are implied through context. In the future, these could be fixed by detecting aberrant situations and encoding the oddity with ASCII. Furthermore, in the future, it'd be great to detect any ASCII character that is not supported and encode it raw too. This could also be extended to allow for Unicode characters.

From here, what we can do is scan every character while encoding the file. If the character is a punctuation point, the 'current token' (prior, unaccounted for string) is appended to tokens (after some sanitisation) and the punctuation mark is appended to tokens. Certain punctuation points have custom functions such as the space not appending itself to. Else, if the character is a capital, mark the current token as a name. If it is a capital or not, concatenate the char to the end of the current token.

## tokenised format
[escape codes] [punctuation] [words] [pairs]

### escape codes
0x00 - initally lowercase toki pona syllable string
0x01 - initally capitalised toki pona syllable string
0x02 - UTF-8 string
0x03 - UTF-16 string

#### initally lowercase toki pona syllable string
[capitalised space] [lowercase space] [syllables] [0xFF - end string]
 - the first syllable is lowercase
 - encodes all 92 valid syllables
 - has two types of spaces that either capitalise or don't capitalise the following syllable
 - counts as a spaced word for automatic spacing rules
 - has an end string key

#### initally capitalised toki pona syllable string
[capitalised space] [lowercase space] [syllables] [0xFF - end string]
 - the first syllable is capitalised
 - encodes all 92 valid syllables
 - has two types of spaces that either capitalise or don't capitalise the following syllable
 - counts as a spaced word for automatic spacing rules
 - has an end string key

#### UTF-8 string
 - followed by a length token for the UTF-8 string length in bytes
 - the first length token is one byte in size
 - if the current length token is its maximum value, then it will be followed by another token of double the size for the real length. this rule applies iteratively to allow for strings of arbitrary length.
 - the length tokens are unsigned little endian integers
 - after that there is an array of encoded UTF-8 bytes
 - counts as unspaced punctuation for automatic spacing rules
 - with a string length of zero, this acts as an automatic space suppressor

#### UTF-16 string
 - followed by a length token for the UTF-16 string length in bytes
 - the first length token is one byte in size
 - if the current length token is its maximum value, then it will be followed by another token of double the size for the real length. this rule applies iteratively to allow for strings of arbitrary length.
 - the length tokens are unsigned little endian integers
 - after that there is an array of encoded UTF-16 bytes
 - counts as unspaced punctuation for automatic spacing rules
 - with a string length of zero, this acts as an automatic space suppressor

## function sections
Text
Tokens
Pairs + Tokens
Header + Bytes

## far future possibilities
Use non-byte-aligned compression such as Huffman encoding or even non-bit-aligned encoding such as arithmetic encoding.
Use an understanding of toki pona grammar to improve compression.
With aritmetic encoding, you could also use per-file token pair coding and store the number of pairs using arithmetically coded unary (1 is much lower precision than 0)
