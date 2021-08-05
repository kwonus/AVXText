using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVText
{
	class FiveBitEncoding
	{
		// These are for Hash64 functionality and related methods taking UINT64
		const UInt64 UseFiveBitEncodingOrThreeBitHash = 0x8000000000000000; // hi-order bit marks hash hash-algorithm; helps determine is if hash can be decoded
		const UInt64 ThreeBitHashLengthMarker = 0x7000000000000000; // represents strlen()-12 [1 to 15 means possbible support for strlen() 13 to 27]
		const UInt64 ThreeBitHashLengthLowBit = 0x1000000000000000; // [represents 1 / strlen() == 13]
		const UInt64 ThreeBitHashVowelCounts = 0x0FFC000000000000;
		const UInt64 ThreeBitHashACountLowBit = 0x0400000000000000;
		const UInt64 ThreeBitHashECountLowBit = 0x0100000000000000;
		const UInt64 ThreeBitHashICountLowBit = 0x0040000000000000;
		const UInt64 ThreeBitHashOCountLowBit = 0x0010000000000000;
		const UInt64 ThreeBitHashUCountLowBit = 0x0004000000000000;
		const UInt64 ThreeBitEncodedConsonants = 0x0003FFFFFFFFFFFF;
		const UInt64 ThreeBitEncodedFirstConsonant = 0x0000800000000000;
		const UInt64 FiveBitEncodedLetters = 0x00FFFFFFFFFFFFFF;
		const UInt64 FiveBitEncodedFirstLetter = 0x0008000000000000;
		const UInt64 ThreeBitEncodingMarkers = ThreeBitHashLengthMarker | ThreeBitHashVowelCounts; // These will be zero if 5-bit-encoding is utilized (when zero, hash is decodable)

		//static std::unordered_map<UINT64, const char*>  hashToString;
		private static Dictionary<UInt64, string> hashToString;

		public static string getHashedString(UInt64 hash)
		{
			return hashToString.ContainsKey(hash) ? hashToString[hash] : null;
		}

		// For Part-of-Speech:
		public static UInt32 EncodePOS(string input7charsMaxWithHyphen)
		{ // input string must be ascii
			var len = input7charsMaxWithHyphen.Length;
			if (len < 1 || len > 7)
				return 0;

			var input = input7charsMaxWithHyphen.Trim().ToLower();
			len = input7charsMaxWithHyphen.Length;
			if (len < 1 || len > 7)
				return 0;

			var encoded = (UInt32)0x0;
			var hyphen = (UInt32)input.IndexOf('-');
			if (hyphen > 0 && hyphen <= 3)
				hyphen <<= 30;
			else if (len > 6)   // 6 characters max if a compliant hyphen is not part of the string
				return 0;
			else
				hyphen = (UInt32)0x0;

			int c = 0;
			char[] buffer = new char[6]; // 6x 5bit characters
			for (var i = 0; i < len; i++)
			{
				var b = input[i];
				switch (b)
				{
					case '-':
						continue;
					case '0':
					case '1':
					case '2':
					case '3':
					case '4':
						b -= '0';
						b += (char)(27);
						break;
				}
				buffer[c++] = b;
			}
			var position = (UInt32)0x02000000;
			for (var i = 0; i < 6 - len; i++)
			{
				position >>= 5;
			}
			for (var i = 0; i < len; i++)
			{
				char letter = (char)(buffer[i] & 0x1F);
				if (letter == 0)
					break;

				encoded |= (UInt32)letter * position;
				position >>= 5;
			}
			return (UInt32)(encoded | hyphen);
		}

		//  For Part-of-Speech:
		public static string DecodePOS(UInt32 encoding)
		{
			char[] buffer = new char[7]; // 6x 5bit characters + 2bits for hyphen position = 32 bits;

			var hyphen = (UInt32)(encoding & 0xC0000000);
			if (hyphen > 0)
				hyphen >>= 30;

			var index = 0;
			for (var mask = (UInt32)(0x1F << 25); mask >= 0x1F; mask >>= 5)
			{
				var digit = encoding & mask >> (5 * (5 - index));
				if (digit == 0)
					continue;
				byte b = (byte)digit;
				if (b <= 26)
					b |= 0x60;
				else
				{
					b -= (byte)27;
					b += (byte)'0';
				}
				if (hyphen == index)
					buffer[index++] = '-';
				buffer[index++] = (char)b;
			}
			var decoded = new StringBuilder(index + 1);
			for (int i = 0; i < index; i++)
				decoded.Append((char)buffer[i]);
			return decoded.ToString();
		}
		// input string must be ascii lowercase; hyphens are ignored
		// - reliability in question when string exceeds length 8 or 12:
		// - size <= 8: Entire ascii character set, but ONLY ascii [0x7F]
		// - size <= 12: Lowercase-ascii-lettes-only [a-z] preserved using 5-bit encoding
		// - size <= 16: values for Digital-AV lexicon ALWAYS hash uniquely using 3-bit encoding, but algorithm tuned for that lexicon
		// - size >= 17: NOT TESTED on strings larger than 16! Validate on your daya or collisions handled in your custom code
		public static UInt64 Hash64(string input)
		{
			if (input == null)
				return 0xFFFFFFFFFFFFFFFF;

			var token = input.Trim().ToLower();
			var len = token.Length;

			UInt64 hash;
			if (len <= 8)
			{
				hash = Encode(token);
				if ((hash & UseFiveBitEncodingOrThreeBitHash) == 0)
					return hash;
			}
			hash = UseFiveBitEncodingOrThreeBitHash; // hi-order bit set marks 5-bit or 3-bit encoding

			int ignore = 0; // e.g. hyphens
			for (var i = 0; i < len; i++)
			{
				var c = token[i];
				if (c < 'a' || c > 'z')
					ignore++;
			}
			if (len - ignore <= 12) // 5-bit-encoding
			{
				char[] buffer = new char[12];    // 12x 5-bit characters
				ignore = 0;
				for (var i = 0; i < len; i++)
				{
					var c = token[i];
					if (c >= 'a' && c <= 'z')
						buffer[i - ignore] = c;
					else
						ignore++;
				}
				var position = FiveBitEncodedFirstLetter;
				len -= ignore;
				for (var i = 0; i < len; i++)
				{
					char letter = (char)((byte)(buffer[i]) & 0x1F);
					if (letter == 0)
						break;

					hash |= (UInt64)letter * position;
					position >>= 5;
				}
			}
			else // 3-bit hashes
			{
				UInt64 A, E, I, O, U;
				A = E = I = O = U = 0;

				var buffer = new byte[16];    // 16x 3bit hashes
				ignore = 0;

				for (var i = 0; i < len; i++)
				{
					if (i - ignore >= 16)
						break;
					var c = token[i];
					switch (c)
					{
						// vowel = 1;
						case 'a':
							if (A < 3) A++;
							buffer[i - ignore] = 0;
							continue;
						case 'e':
							if (E < 3) E++;
							buffer[i - ignore] = 0;
							continue;
						case 'i':
							if (I < 3) I++;
							buffer[i - ignore] = 0;
							continue;
						case 'o':
							if (O < 3) O++;
							buffer[i - ignore] = 0;
							continue;
						case 'u':
							if (U < 3) U++;
							buffer[i - ignore] = 0;
							continue;
						case 'b':
						case 'd':
						case 'f':
						case 'g':
						case 'p':
							buffer[i - ignore] = 2;
							continue;

						case 'h':
						case 'y':
						case 'r':
						case 'j':
						case 'l':
							buffer[i - ignore] = 3;
							continue;
						case 'w':
						case 'v':
						case 'm':
						case 'n':
							buffer[i - ignore] = 4;
							continue;
						case 'x':
						case 'z':
							buffer[i - ignore] = 5;
							continue;
						case 'q':
						case 'k':
						case 'c':
							buffer[i - ignore] = 6;
							continue;
						case 't':
							buffer[i - ignore] = 7;
							continue;
						case 's':
							buffer[i - ignore] = 1;
							continue;
						default:
							ignore++;
							break;
					}
				}
				len -= ignore;
				if (len > 16)
					len = 16;

				hash += (A * ThreeBitHashACountLowBit);
				hash += (E * ThreeBitHashECountLowBit);
				hash += (I * ThreeBitHashICountLowBit);
				hash += (O * ThreeBitHashOCountLowBit);
				hash += (U * ThreeBitHashUCountLowBit);

				var position = ThreeBitEncodedFirstConsonant;
				for (var i = 0; i < len; i++)
				{
					var letter = (byte)(((byte)buffer[i]) & 0x07);
					hash |= (UInt64)letter * position;
					position >>= 3;
				}
				if (hashToString.ContainsKey(hash))
				{   // don't allow collisions // validate!!!
					var previous = hashToString[hash];
					int t, p;
					for (t = p = 0; token[t] != 0 && previous[p] != 0; t++, p++)
					{
						while (token[t] == '-')
							if (token[++t] == 0)
								return 0;
						while (previous[p] == '-')
							if (previous[++p] == 0)
								return 0;
						if (token[t] != previous[p])
							return 0;
					}
				}
				else hashToString[hash] = token;
			}
			return hash;
		}
		unsafe public static UInt64 Encode(string input, bool normalize)
		{ // input string must be ascii and cannot exceed 8
			UInt64 hash = 0;
			byte* chash = (byte*)&hash;

			int len = input.Length;
			if (len > 0)
			{
				string norm = normalize ? input.ToLower() : null;
				for (int i = 0; i < sizeof(UInt64) && i < len; i++) // sizeof(UINT64) == 8
					*chash++ = (byte)(normalize ? norm[i] : input[i]);
			}
			return hash;
		}
		public static UInt64 Encode(string c)
		{ // input string must be ascii and cannot exceed 8
			return Encode(c, true);
		}
		public static int fivebitencodedStrlen(UInt64 hash)
		{
			if ((hash & UseFiveBitEncodingOrThreeBitHash) == 0)
				return -1;
			if ((hash & ThreeBitHashLengthMarker) != 0)
				return -1;

			UInt64 bits = 0x1F * FiveBitEncodedFirstLetter;
			for (var i = 0; i < 12; i++)
				if ((hash & bits) == 0)
					return i;

			return 12;
		}
		public static int threebitencodedStrlen(UInt64 hash)
		{
			if ((hash & UseFiveBitEncodingOrThreeBitHash) == 0)
				return -1;
			if ((hash & ThreeBitHashLengthMarker) != 0)
				return -1;

			var len = ThreeBitHashLengthMarker & hash;
			len /= ThreeBitHashLengthLowBit;

			return (int)len;
		}
		public static string DecodeFiveBitHash(UInt64 hash)
		{
			if ((hash & UseFiveBitEncodingOrThreeBitHash) == 0)
			{
				return null;
			}
			int clen = fivebitencodedStrlen(hash);
			var buffer = new StringBuilder(clen);

			var position = FiveBitEncodedFirstLetter;
			for (var i = 0; i < clen; i++)
			{
				UInt64 letter = (hash & (position * 0x1F)) / position;
				buffer.Append((char)('a' + (char)(letter - 1)));
				position >>= 5;
			}
			return buffer.ToString();
		}
		unsafe private static uint strlen(char* str, uint max)
		{
			if (str == null)
				return 0;
			for (uint i = 0; i < max; i++)
				if (str[i] == 0)
					return i;
			return max;
		}
		unsafe public static string DecodeAscii(UInt64 hash)
		{
			if ((hash & UseFiveBitEncodingOrThreeBitHash) != 0)
				return null;

			var buffer = new StringBuilder(sizeof(UInt64));
			char* chash = (char*)&hash;
			//	bytes are left-justified
			//
			uint clen = strlen(chash, sizeof(UInt64));

			int i;
			for (i = 0; i < clen; i++)
				buffer.Append(chash[i]);

			return buffer.ToString();
		}
		public static string LookupThreebitBitHash(UInt64 hash)
		{
			if (hashToString.ContainsKey(hash))
				return hashToString[hash];

			return null;
		}
		public static string Decode(UInt64 hash)
		{
			if ((hash & UseFiveBitEncodingOrThreeBitHash) == 0)
				return DecodeAscii(hash);

			if ((hash & ThreeBitHashLengthMarker) == 0)
				return DecodeFiveBitHash(hash);

			string result = LookupThreebitBitHash(hash);

			return result;
		}
	}
}
