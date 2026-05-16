# theory
Encoding and decoding will be two step processes: conversions between text, tokens and bytes.

## tokenisation
This will require a word dictionary and a punctuation dictionary. The punctuation dictionary encodes info on punctuation spacing.

The text will be assumed to be made mostly of punctuation, spaces and known toki pona words. Any other type of text will need to be encoded as toki pona syllables, ASCII or UTF-8.

On encoding text into characters, we must use a two step process. Firstly, we encode the text into tokens that ignore the automatic spacing rules used by decompression and then we predict these spaces and remove any that will be inferred.

## compression
When serializing or deserializing the tokens, I plan to add additional compression. I will add pair comperssion that uses the spare tokens above the section which adress escape codes, punctuation and toki pona words. I will store a 'pair count' byte at the start of the file followed by two bytes per pair. I will also use a seperate pair list for pair encoding the ASCII characters within ASCII strings and I plan to also have another list for pair encoding within toki syllable strings.

A major issue with using pair encoding in this way is that when compressing very large texts, you quickly run out of spaces for more pairs that could massively improve compression. So, I will also include an escape code for reintroducing a pair header. This escape string will start with a flag byte for which of the three pair sets you are going to re-encode.
One serious difficluty with this solution is knowing when to re-encode pairs. Initially, I will implement this using a simple heuristic but I may eventually do a serious comparison between various methods with several large test texts.

## far future possibilities
Use non-byte-aligned compression such as Huffman encoding or even non-bit-aligned encoding such as arithmetic encoding.
Use an understanding of toki pona grammar to improve compression.
With aritmetic encoding, you could also use per-file token pair coding and store the number of pairs using arithmetically coded unary (1 is much lower precision than 0)
