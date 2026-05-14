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

# compressed format
[escape codes] [punctuation] [words] [pairs]

## pair encoding
The file starts with an encoding of pairs for tokens, syllables and ascii (in that order). Each pair set starts with a length byte that declares the number of pairs of that type (the types being tokens, syllables and ascii pairs). The following length\*2 bytes are pair data. Each pair is made of two ordered bytes - the new pair token will be substituted for these bytes recursively. By that, I mean that one pair can include a reference other pairs and they will all be unpacked sequentially.
Later, pairs can be re-encoded by using the update pair data escape code.

## escape codes
0x00 - update pair data
0x01 - insert space
0x02 - initially lowercase toki pona syllable string
0x03 - initially capitalised toki pona syllable string
0x04 - ASCII string
0x05 - UTF-16 string

### update pair data
 - followed by a flag byte that determines which pair types should be re-encoded
 - for the flag byte, the lowest bit is tokens, the second bit is syllables and the third lowest big is ascii
 - this is then followed by a number of pairs followed by a list of those pairs which is then repeated for each pair type that has been selected for re-encoding (ordered tokens, syllables, ascii)

### insert space
 - literally just encodes a space in one byte
 - this token should be ignored by the space prediction algorithm to allow a double space in a place where only one space was predicted to be only one byte for no extra cost

### initially lowercase toki pona syllable string
[0x00 - end string] [capitalised space] [lowercase space] [syllables]
 - the first syllable is lowercase
 - encodes 100 different syllables
 - each syllable can be understood as a mixed radix number. the least significant digit, the coda flips between every syllable between being absent and present ordered as "-n" (with '-' meaning absent). every full cycle of the coda, the vowel advances through its cycle ordered as "aeiou". every full cycle of the the vowels, the consonant advances through its cycle "-jklmnpstw". the system has a total of 10 * 5 * 2 = 100 states. the first syllable would be "a" and the last syllable would be "wun"
 - has two types of spaces that either capitalise or don't capitalise the following syllable
 - counts as a spaced word for automatic spacing rules
 - has an end string key

### initially capitalised toki pona syllable string
[0x00 - end string] [capitalised space] [lowercase space] [syllables] [pair encoding space]
 - the first syllable is capitalised
 - encodes all 100 different syllables
 - each syllable can be understood as a mixed radix number. the least significant digit, the coda flips between every syllable between being absent and present ordered as "-n" (with '-' meaning absent). every full cycle of the coda, the vowel advances through its cycle ordered as "aeiou". every full cycle of the the vowels, the consonant advances through its cycle "-jklmnpstw". the system has a total of 10 * 5 * 2 = 100 states. the first syllable would be "a" and the last syllable would be "wun"
 - has two types of spaces that either capitalise or don't capitalise the following syllable
 - counts as a spaced word for automatic spacing rules
 - has an end string key

### ASCII string
 - followed by an array of ASCII bytes
 - terminated with null (0x00)
 - if you want to encode null, use 0x80
 - anything above 0x80 can be used for pair encoding
 - counts as unspaced punctuation for automatic spacing rules
 - with a string length of zero, this acts as an automatic space suppressor

### UTF-16 string
 - followed by an array of encoded UTF-16 bytes
 - terminated with null (0x00)
 - does not support pair encoding
 - counts as unspaced punctuation for automatic spacing rules
 - with a string length of zero, this acts as an automatic space suppressor

# implementation
## features
### done
 - add base token encoding/decoding
 - add ascii string encoding/decoding
 - add intermediary tokenization system
 - add pair compression system

### todo
 - add toki pona string encoding/decoding
 - add UTF-16 encoding/decoding
 - add insert space encoding/decoding
 - add update pair header encoding/decoding

## structure
### todo
 - take everything in the TokiCodex class and move it to be in a namespace with several classes for easier encapsulation
