using System;
using System.Collections;
using System.Collections.Generic;

using System.Diagnostics;
using QuelleHMI;
using QuelleHMI.Tokens;
using QuelleHMI.Interop;
using AVText;
using System.Text;
using QuelleHMI.Definitions;
using QuelleHMI.Actions;
using System.IO;

namespace AVSDK
{
	public class AVXAPI : ISearchProvider
	{
		// Primary Byte (orthography-based features):
		public const byte FIND_Token_MASK = 0xC0;
		public const byte FIND_Token = 0x80; // examples: ran walked I
		public const byte FIND_Token_WithWildcard = 0xC0; // examples: run* you*#modern
		public const byte FIND_Suffix_MASK = 0x30;
		public const byte FIND_Suffix_Exact = 0x10; // examples: #exact #kjv #av
		public const byte FIND_Suffix_Modern = 0x20; // examples: #avx #modern
		public const byte FIND_Suffix_Either = 0x30; // examples: #any #fuzzy
		public const byte FIND_Suffix_None = 0x00; // use global setting search.exact, search.fuzzy, or search.modern
		public const byte FIND_Lemma = 0x08; // examples: #run
		public const byte FIND_GlobalTest = 0x04; // examples: #diff
		public const byte FIND_LANGUAGE_NUMERIC = 0x03; // regex: #[0-9]*[ehg]
		public const byte FIND_English = 0x03; // examples: #12345e
		public const byte FIND_Greek = 0x02; // examples: #12345g
		public const byte FIND_Hebrew = 0x01; // examples: #12345h

		// Secondary Byte (other linguistic features /delimited by slashes/):
		public const byte SLASH_Boundaries = 0x08; // examples: /BoV/ /BoC/ /EoB/
		public const byte SLASH_Puncuation = 0x04; // examples: /;/ /./ /?/ /'/
		public const byte SLASH_DiscretePOS = 0x02; // examples: /av/ /dt/ /n2-vhg/
		public const byte SLASH_BitwisePOS = 0x01; // examples: /-01-/

		public const byte SLASH_RESERVED_80 = 0x80;
		public const byte SLASH_RESERVED_40 = 0x40;
		public const byte SLASH_RESERVED_20 = 0x20;
		public const byte SLASH_RESERVED_10 = 0x10;

#if NEVER
		public const byte SEARCH = 1;   // sequence
		public const byte DISPLAY = 2; // sequence
		public const byte MODERN = 3;  // sequence
		public const byte MODERN_WITHOUT_HYPHENS = 4;  // sequence
#endif

		private static Dictionary<UInt64, byte> globalMap = new Dictionary<UInt64, byte>();   // examples: #diff
		private static Dictionary<UInt64, byte> boundaryMap = new Dictionary<UInt64, byte>();    // examples: /BoV/ /BoC/ /EoB/
		private static Dictionary<UInt64, byte> punctuationMap = new Dictionary<UInt64, byte>();      // examples: /;/ /./ /?/ /'/
		private static Dictionary<UInt64, byte> suffixMap = new Dictionary<UInt64, byte>();  // examples: #kjv[1] #av[1] #exact[1] #avx[2] #modern[2] #any[3] #fuzzy[3]
		private static Dictionary<UInt16, string> LemmaOovMap;
		private static Dictionary<UInt64, AVText.AVLemma> LemmaMap;
		private static Dictionary<UInt16, AVText.AVLexicon> LexiconMap;
		private static Dictionary<UInt16, AVText.AVWordClass> WclassMap;
		private static Dictionary<UInt16, AVText.AVName> NamesMap;
		private static Dictionary<UInt16, string> ForwardLemmaMap;
		private static Dictionary<UInt64, AVText.Bucket> ReverseLemmaMap;
		private static Dictionary<UInt64, AVText.Bucket> ReverseModernMap;
		private static Dictionary<UInt64, UInt16> ReverseSearchMap;
		private static Dictionary<UInt64, UInt16> ReverseNameMap;
		private static Dictionary<UInt64, byte> SlashBoundaryMap;   // examples: /BoV/ /BoC/ /EoB/
		private static Dictionary<UInt64, byte> SlashPuncMap;       // examples: /;/ /./ /?/ /'/
		private static Dictionary<UInt64, byte> PoundWordSuffixMap; // examples: #kjv[1] #av[1] #exact[1] #avx[2] #modern[2] #any[3] #fuzzy[3]
		private static Dictionary<UInt64, byte> PoundWordlessMap;   // examples: #diff
		public static AVXAPI SELF = null;
		
		public MMWritDX11 XWrit;
		public IXBook XBook;
		public IXChapter XChapter;
		public IXVerse XVerse;

		public Chapter[] Chapters;
		public Book[] Books;
		public UInt32[] Verses;

		public AVXAPI()
		{
			//
			//TODO: Add the constructor code here (and get rif of hard-coded path)
			//
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Digital-AV");
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);

			this.XWrit = new MMWritDX11(path);
			this.XBook = new IXBook(path);
			this.XVerse = new IXVerse(path);
			this.XChapter = new IXChapter(path);


			this.Books = XBook.books;
			this.Chapters = XChapter.chapters;
			this.Verses = XVerse.verses;

			SELF = this;
		}
		public IQuelleSearchResult Search(IQuelleSearchRequest request)
        {
			var result = this.CompileSearchRequest(request);
			if (request.clauses.Length > UInt16.MaxValue) {
				result.AddError("A maximum of 14 search segments are supported by this library.");
				return result;	// currently only supports a maximum of 14 search segments
			}
			UInt64 fcnt = 0; // count features
			foreach (var clause in request.clauses)
				fcnt += (UInt64)(clause.fragments.Length);
			if (fcnt > 64) {
				result.AddError("A maximum of 64 search fragments are supported by this library.");
				return result; // currently only supports a maximum of 64 search fragments
			}

			UInt64 f = 0x1;
			foreach (var clause in request.clauses)
				foreach (var frag in clause.fragments)
				{
					((QSearchFragment)frag).bit = f;
					f <<= 1;
					if (f == 0) // not perfectly gracefully, handle overflow; UI will not be able to display correct token (or it will be ambiguous; we only support 64 tokens
						f = 0x1;
				}

			foreach(var clause in request.clauses)
				if (clause.polarity == '+')
					this.ExecuteSearchRequest(clause, request.controls, result);

			foreach(var clause in request.clauses)
				if (clause.polarity == '-')
					this.ExecuteSearchRequest(clause, request.controls, result);

			return result;
        }

		public IQuellePageResult Page(IQuellePageRequest request)
		{
			return null;
		}
		(List<UInt16> list, byte tokenType, byte otherType, UInt32 pos, string error) EncodeAny(String token)
		{
			(List<UInt16> list, byte tokenType, byte otherType, string error)? result = null;
			(byte tbd, byte discretePOS, UInt32 pos)? result32;
			UInt32 pos = 0;
			if (token.StartsWith("/") && token.EndsWith("/"))
			{
				if (token.Length > 2)
				{
					String test = token.Substring(0, token.Length - 2);
					var hash = this.Encode64(test);

					result = hash != 0 ? this.EncodeBoundary(hash) : null;
					if (result == null || (result.Value.otherType == 0 && result.Value.error != null))
						result = EncodePunctuation(hash);
					if (result == null || (result.Value.otherType == 0 && result.Value.error != null))
						result = EncodeBitwisePOS(test);
					if (result == null || (result.Value.otherType == 0 && result.Value.error != null))
					{
						result32 = EncodeDiscretePOS(test);
						if (result32 != null)
						{
							pos = result32.Value.pos;
							result = (null, result32.Value.tbd, result32.Value.discretePOS, null);
						}
					}
				}
			}
			else if (token.StartsWith("#"))
			{
				UInt64 hash = this.Encode64(token);
				result = hash != 0 ? EncodeGlobalTest(hash) : null;
				if (result == null || (result.Value.tokenType == 0 && result.Value.error != null))
					result = EncodeLemma(token);
				if (result == null || (result.Value.tokenType == 0 && result.Value.error != null))
					result = EncodeLanguageNumeric(token);
			}
			else
			{
				result = EncodeWord(token);
			}
			return result.HasValue ? (result.Value.list, result.Value.tokenType, result.Value.otherType, pos, result.Value.error)
								   : (null, 0, 0, 0, "Invalid method invocation");
		}
		(List<UInt16> list, byte tokenType, byte otherType, string error)? EncodeGlobalTest(UInt64 hash)
		{
			String  error = null;
			Byte tokenType = 0;
			Byte otherType = 0;
			var list = new List<UInt16>();

			if (globalMap.ContainsKey(hash))
			{
				list.Add(globalMap[hash]);
				tokenType = FIND_GlobalTest;
			}
			var result = (list, tokenType, otherType, error);
			return result;
		}
		(List<UInt16> list, byte tokenType, byte otherType, string error)? EncodeBoundary(UInt64 hash)
		{
			String  error = null;
			Byte tokenType = 0;
			Byte otherType = 0;
			var list = new List<UInt16>();

			if (globalMap.ContainsKey(hash))
			{
				list.Add(globalMap[hash]);
				tokenType = FIND_GlobalTest;
			}
			var result = (list, tokenType, otherType, error);
			return result;
		}
		(List<UInt16> list, byte tokenType, byte otherType, string error)? EncodePunctuation(UInt64 hash)
		{
			String  error = null;
			Byte tokenType = 0;
			Byte otherType = 0;
			var list = new List<UInt16>();

			if (punctuationMap.ContainsKey(hash))
			{
				list.Add(punctuationMap[hash]);
				otherType = SLASH_Puncuation;
			}
			var result = (list, tokenType, otherType, error);
			return result;
		}
		(List<UInt16> list, byte tokenType, byte otherType, string error)? EncodeLanguageNumeric(String  token)
		{
			var list = new List<UInt16>();

			if (token.StartsWith("#") && token.Length >= 3)
			{
				byte lang = 0;
				switch (char.ToLower(token[token.Length - 1]))
				{
					case 'e': lang = FIND_English; break;
					case 'h': lang = FIND_Hebrew; break;
					case 'g': lang = FIND_Greek; break;
				}
				if (lang != 0)
				{
					for (int i = 1; i < token.Length - 1; i++)
						if (token[i] < '0' || token[i] > '9')
							return (null, 0, 0, null);
					list.Add(UInt16.Parse(token.Substring(1, token.Length)));
					return (list, lang, 0, null);
				}
			}
			return (null, 0, 0, null);
		}
		(byte tbd, byte discretePOS, UInt32 pos)? EncodeDiscretePOS(String  token)
		{
			var list = new List<UInt16>();

			var hyphen1 = token.IndexOf('-');
			var hyphen2 = token.LastIndexOf('-');
			var len = token.Length;
			if (len > 7)
				return null;
			if (hyphen1 < 0 && len > 6)
				return null;
			if (hyphen1 > 0 && hyphen1 != hyphen2)
				return null;
			if (hyphen1 > 3)
				return null;

			unsafe
			{
				int nums = 0;
				int letters = 0;
				for (int i = 0; i < len; i++)
				{
					var c = token[i];
					if (c >= '1' && c <= '2')
						nums++;
					else if (c >= 'a' && c <= 'z')
						letters++;
					else if (c != '-')
						return null;
				}
				if (nums > 1 || letters < 1)
					return null;
				var posHash = FiveBitEncoding.EncodePOS(token);
				return (0, SLASH_DiscretePOS, posHash);
			}
		}
		(List<UInt16> list, byte tokenType, byte otherType, string error)? EncodeBitwisePOS(String  token)
			{
			var list = new List<UInt16>();

			var len = token.Length;
			if (len != 4)
				return (null, 0, 0, null);
			UInt16 bits = 0;
			for (int i = 0; i < 4; i++)
			{
				bits <<= 4;
				char c = char.ToLower((char)token[i]);
				if (c != '-' || c != '0')
				{
					if (c >= 1 && c <= 9)
						bits += (UInt16)(c - '0');
					else if (c >= 'a' && c <= 'f')
						bits += (UInt16)(10 + (c - 'a'));
					else
						return (null, 0, 0, null);
				}
			}
			list.Add(bits);
			return (list, SLASH_BitwisePOS, 0, null);
		}
		(List<UInt16> list, byte tokenType, byte otherType, string error)? EncodeLemma(String  token)
		{
			if (!token.StartsWith("#"))
			{
				return (null, 0, 0, null);
			}
			String  error = null;
			Byte tokenType = FIND_Lemma;
			Byte otherType = 0;
			var list = new List<UInt16>();

			//UInt64 test = this.Hash64(token);
			if (AVLemma.OOVLemmaReverseMap.ContainsKey(token))
			{
				UInt16 lemma = AVLemma.OOVLemmaReverseMap[token];
			}
			else
			{
				error = "Token appears to represent a lemma, but the lemma was not found.";
			}
			var result = (list, tokenType, otherType, error);
			return result;
		}

		(List<UInt16> list, byte tokenType, byte otherType, string error)? EncodeWord(String  token)
		{
			var list = new List<UInt16>();

			String  error = null;
			byte tokenType = FIND_Token;
			byte otherType = 0;

			bool good = true;
			var pound = token.IndexOf('#');
			String  word;
			if (pound >= 0)
			{
				good = false;
				var chk = this.Encode64(token.Substring(pound));
				good = suffixMap.ContainsKey(chk);
				if (good)
					tokenType |= suffixMap[chk];
				else
					error = "A suffix operator is apperently being applied to the word toke, but '" + token.Substring(pound) + "' is an unknown suffix.";
				word = pound > 0 ? token.Substring(0, pound) : "";
			}
			else word = token;
			if (good)
			{
				var star = word.IndexOf('*');
				if (star >= 0)
				{
					tokenType |= FIND_Token_WithWildcard;

					var hyphen = word.IndexOf('-');
					String  test = hyphen >= 0 ? word.Replace("-", "").ToLower() : word.ToLower();
					star = test.IndexOf('*');

					int len = test.Length;
					String  start = star > 0 ? test.Substring(0, star) : null;
					String  end = star < len - 1 ? test.Substring(star + 1) : null;

					switch (tokenType & FIND_Suffix_MASK)
					{
						case FIND_Suffix_Modern:
							EncodeWordSearchWildcard(list, start, end);
							break;
                        case FIND_Suffix_Either:
							EncodeWordModernWildcard(list, start, end);
							break;
						case FIND_Suffix_Exact:
						default: EncodeWordSearchWildcard(list, start, end);
							break;
					}
					if (list.Count == 0)
						error = "No matches found with wildcard: " + token;
				}
				else
				{
					//UInt64 hash = this.Hash64(token); /// <--- Error occurs here
					switch (tokenType & FIND_Suffix_MASK)
					{
						case FIND_Suffix_Modern:
							EncodeWordSearch(list, token);
							break;
						case FIND_Suffix_Either:
							EncodeWordModern(list, token);
							break;
						case FIND_Suffix_Exact:
						default: EncodeWordSearch(list, token);
							break;
					}
					if (list.Count == 0)
						error = "Token appears to represent a word, but the word was not found: " + token;
				}
			}
			return (list, tokenType, otherType, error);
		}
		// TODO: Review, this is likely wrong:
		void EncodeWordModernWildcard(List<UInt16> list, String start, String end)
		{
			foreach (var item in AVXAPI.ReverseModernMap)
			{
				Bucket bucket = item.Value;
				UInt64 hashed = bucket.value;
				string token = FiveBitEncoding.Decode(hashed);
				if ((token != null)
					&& (start == null || token.StartsWith(start))
					&& (end   == null || token.EndsWith(end)))
				{
					list.Add(bucket.value);
					for (var overflow = bucket.GetOverflow(); overflow != null; overflow = overflow.next)
						list.Add(overflow.value);
				}
			}
		}
		// TODO: Review, this is likely wrong:
		void EncodeWordSearchWildcard(List<UInt16> list, String start, String end)
		{
			foreach (var item in AVXAPI.ReverseSearchMap)
			{
				UInt64 hashed = item.Key;
				string token = FiveBitEncoding.Decode(hashed);
				if ((token != null)
					&& (start == null || token.StartsWith(start))
					&& (end == null || token.EndsWith(end)))
					list.Add(item.Value);
			}
		}
		bool EncodeWordSearch(List<UInt16> list, string token)
		{
			//var ortho = Decode64(word);

			if (token != null)
			{
				var found = AVLexicon.GetReverseLex(token);

				if (found != 0)
				{
					list.Add(found);
					return true;
				}
			}
			return false;
		}
		bool EncodeWordModern(List<UInt16> list, string token)
		{
			UInt16[] keys = AVLexicon.GetReverseLexModern(token);
			if (keys != null)
				foreach (var key in keys)
					list.Add(key);

			return keys != null;
		}
		AbstractQuelleSearchResult  CompileSearchRequest(IQuelleSearchRequest  request)
		{
			var result = new AbstractQuelleSearchResult();
			foreach (var clause in request.clauses) {
#if AVX_EXTRA_DEBUG_DIAGNOSTICS
				Console::Out.WriteLine(clause.segment + ": (compilation)");
#endif
				foreach (var fragment in clause.fragments) {
					if (fragment.text.StartsWith("|") || fragment.text.EndsWith("|"))
					{
						result.AddError("The '|' logical-or operator cannot be used without left and right operands");
						return result;
					}
					else if (fragment.text.StartsWith("&") || fragment.text.EndsWith("&"))
					{
						result.AddError("The '&' logical operator-and cannot be used without left and right operands");
						return result;
					}
					else if (fragment.text == null || fragment.text.Length < 1)
					{
						result.AddError("Unable to parse word-token specication");
						return result;
					}
					foreach (var spec in fragment.specifications) {
						foreach (var match in spec.matchAny) {
							foreach (var feature in match.features) {
								var tuple = EncodeAny(feature.feature);
								if (tuple.error != null)
									result.AddError(tuple.error);
								else if (tuple.tokenType == 0 && tuple.otherType == 0)
									result.AddError("Could not parse feature token: '" + feature.feature + "'");
								else if (tuple.pos != 0)
								{
									((Feature )feature).type = tuple.tokenType;
									((Feature )feature).subtype = tuple.otherType;
									((Feature )feature).discretePOS = tuple.pos;
									((Feature )feature).featureMatchVector = null;
								}
								else if (tuple.list != null && tuple.Item1.Count > 0)
								{
									((Feature )feature).type = tuple.tokenType;
									((Feature )feature).subtype = tuple.otherType;
									((Feature )feature).discretePOS = 0;
									((Feature )feature).featureMatchVector = tuple.list;
								}
								else if (tuple.tokenType == 0 && tuple.otherType == 0)
									result.AddError("Could not parse feature token: '" + feature.feature + "'");
							}
						}
					}
				}
			}
			return result;
		}
		void ExecuteSearchRequest(IQuelleSearchClause clause, IQuelleSearchControls controls, IQuelleSearchResult result)
		{
#if AVX_EXTRA_DEBUG_DIAGNOSTICS
			Console::Out.WriteLine(clause.segment + ": (execution)");
#endif
			var bcv = new BookChapterVerse();
			bcv.SearchClause(clause, controls);
			result.segments = bcv.Matches;
			result.tokens = bcv.Tokens;
		}
		// godhead + -- "eternal power"
		IQuelleSearchResult  Search(QRequestSearch  request)
		{
			var result = this.CompileSearchRequest(request);
			if (request.clauses.Length > UInt16.MaxValue)
			{
				result.AddError("A maximum of 14 search segments are supported by this library.");
				return result;  // currently only supports a maximum of 14 search segments
			}
			UInt64 fcnt = 0; // count features
			foreach(var clause in request.clauses)
				fcnt += (UInt64) clause.fragments.Length;
			if (fcnt > 64)
			{
				result.AddError("A maximum of 64 search fragments are supported by this library.");
				return result; // currently only supports a maximum of 64 search fragments
			}

			UInt64 f = 0x1;
			foreach (var clause in request.clauses)
				foreach (var frag in clause.fragments)
				{
					((QSearchFragment)frag).bit = f;
					f <<= 1;
					if (f == 0) // not perfectly gracefully, handle overflow; UI will not be able to display correct token (or it will be ambiguous; we only support 64 tokens
						f = 0x1;
				}

			foreach (var clause in request.clauses)
					if (clause.polarity == '+')
					this.ExecuteSearchRequest(clause, request.controls, result);

			foreach (var clause in request.clauses)
					if (clause.polarity == '-')
					this.ExecuteSearchRequest(clause, request.controls, result);

			return result;
		}
		IQuellePageResult  Page(QRequestPage  request)
			{
			return null;
		}
		String Test(String request)
		{
			return null;
		}
		UInt64 Hash64(String token)
		{
			return FiveBitEncoding.Hash64(token);
		}
		UInt64 Encode64(String token)
		{
			if (token.Length > 8)
				return 0;

			return FiveBitEncoding.Encode(token);
		}
		UInt64 Encode64(String token, bool normalize)
		{
			if (token.Length > 8)
				return 0;

			return FiveBitEncoding.Encode(token, normalize);
		}
		String Decode64(UInt64 hash)
		{
			var result = FiveBitEncoding.Decode(hash);
			return result;
		}
	}
}
