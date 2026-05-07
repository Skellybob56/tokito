# theory
Encoding and decoding will be two step processes: conversions between text, tokens and bytes.

## tokenisation
This will require a word dictionary and a punctuation dictionary. The punctuation dictionary encodes info on punctuation spacing.

The text will be assumed to be made mostly of punctuation, spaces and known toki pona words. Any other type of text will need to be encoded as toki pona syllables, ASCII or even UTF-16.

On encoding text into characters, we must use a two step process. Firstly, we encode the text into tokens that ignore the automatic spacing rules used by decompression and then we predict these spaces and remove any that will be inferred.

## compression
When serializing or deserializing the tokens, I plan to add additional compression. I will add pair comperssion that uses the spare tokens above the section which adress escape codes, punctuation and toki pona words. I will store a 'pair count' byte at the start of the file followed by two bytes per pair. I will also use a seperate pair list for pair encoding the ASCII characters within ASCII strings and I plan to also have another list for pair encoding within toki syllable strings.

A major issue with using pair encoding in this way is that when compressing very large texts, you quickly run out of spaces for more pairs that could massively improve compression. So, I will also include an escape code for reintroducing a pair header. This escape string will start with a flag byte for which of the three pair sets you are going to re-encode.
One serious difficluty with this solution is knowing when to re-encode pairs. Initially, I will implement this using a simple heuristic but I may eventually do a serious comparison between various methods with several large test texts.

## far future possibilities
Use non-byte-aligned compression such as Huffman encoding or even non-bit-aligned encoding such as arithmetic encoding.
Use an understanding of toki pona grammar to improve compression.
With aritmetic encoding, you could also use per-file token pair coding and store the number of pairs using arithmetically coded unary (1 is much lower precision than 0)

# tokenised format
[escape codes] [punctuation] [words] [pairs]

## escape codes
0x00 - reintroduce pair encoding header
0x01 - space token
0x02 - initally lowercase toki pona syllable string
0x03 - initally capitalised toki pona syllable string
0x04 - ASCII string
0x05 - UTF-16 string

### reintroduce pair encoding header
 - followed by a flag byte that determines which pair types should be reincoded

### space token
 - literally just encodes a space in one byte

### initally lowercase toki pona syllable string
[capitalised space] [lowercase space] [syllables] [0xFF - end string]
 - the first syllable is lowercase
 - encodes all 92 valid syllables
 - has two types of spaces that either capitalise or don't capitalise the following syllable
 - counts as a spaced word for automatic spacing rules
 - has an end string key

### initally capitalised toki pona syllable string
[capitalised space] [lowercase space] [syllables] [0xFF - end string]
 - the first syllable is capitalised
 - encodes all 92 valid syllables
 - has two types of spaces that either capitalise or don't capitalise the following syllable
 - counts as a spaced word for automatic spacing rules
 - has an end string key

### UTF-8 string
 - followed by a length token for the UTF-8 string length in bytes
 - the first length token is one byte in size
 - if the current length token is its maximum value, then it will be followed by another token of double the size for the real length. this rule applies iteratively up to a 4 byte length token to allow for strings of great length. a 4 byte length token is always taken literally.
 - the length tokens are unsigned little endian integers
 - after that there is an array of encoded UTF-8 bytes
 - counts as unspaced punctuation for automatic spacing rules
 - with a string length of zero, this acts as an automatic space suppressor

### UTF-16 string
 - followed by a length token for the UTF-16 string length in units of 2 bytes
 - the first length token is one byte in size
 - if the current length token is its maximum value, then it will be followed by another token of double the size for the real length. this rule applies iteratively up to a 4 byte length token to allow for strings of great length. a 4 byte length token is always taken literally.
 - the length tokens are unsigned little endian integers
 - after that there is an array of encoded UTF-16 bytes
 - counts as unspaced punctuation for automatic spacing rules
 - with a string length of zero, this acts as an automatic space suppressor
