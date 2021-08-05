using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QuelleHMI;
using QuelleHMI.Tokens;
using QuelleHMI.Interop;
using AVSDK;
using QuelleHMI.Definitions;
using QuelleHMI.Actions;

namespace AVText
{
    class BookChapterVerse
    {
		public HashSet<UInt64> Matches;
		public Dictionary<UInt32, UInt64> Tokens;

		public BookChapterVerse() {
			this.Matches = new HashSet<UInt64>();	// this could be managed by Maganimity
			this.Tokens = new Dictionary<UInt32, UInt64>();
		}

		public bool AddMatch(UInt16 segmentIdx, UInt32 wstart, UInt32 wlast)
		{
			var encoded = SegmentElement.Create(wstart, wlast, segmentIdx);
			if (!this.Matches.Contains(encoded))
				this.Matches.Add(encoded);

			return true;
		}
		bool AddMatch(UInt16 segmentIdx, UInt32 wstart, UInt16 wcnt)
		{
			var encoded = SegmentElement.Create(wstart, wcnt, segmentIdx);
			if (!this.Matches.Contains(encoded))
				this.Matches.Add(encoded);

			return true;
		}
		bool SubtractMatch(UInt32 wstart, UInt32 wlast)
		{
			var list = new List<UInt64>();

			foreach (var encoded in this.Matches)
			{
				var estart = SegmentElement.GetStart(encoded);
				var elast = SegmentElement.GetStart(encoded);

				if (estart >= wstart && estart <= wlast && elast <= wlast && elast >= wstart)
					list.Add(encoded);
			}
			foreach (var encoded in list)
			{
				this.Matches.Remove(encoded);
			}
			return true;
		}
		bool SubstractMatch(UInt32 wstart, UInt16 wcnt)
		{
			var list = new List<UInt64>();
			UInt32 wlast = wstart + wcnt - 1;

			foreach (var encoded in this.Matches)
			{
				var estart = SegmentElement.GetStart(encoded);
				var elast = SegmentElement.GetStart(encoded);

				if (estart >= wstart && estart <= wlast && elast <= wlast && elast >= wstart)
					list.Add(encoded);
			}
			foreach (var encoded in list)
			{
				this.Matches.Remove(encoded);
			}
			return true;
		}

		public void SearchClause(IQuelleSearchClause clause, IQuelleSearchControls controls)
		{
			if (clause.quoted)
			{
				if (controls.span == 0)
					this.SearchClauseQuoted_ScopedUsingVerse(clause, controls);
				else
					this.SearchClauseQuoted_ScopedUsingSpan(clause, controls);
			}
			else
			{
				if (controls.span == 0)
					this.SearchClauseUnquoted_ScopedUsingVerse(clause, controls);
				else
					this.SearchClauseUnquoted_ScopedUsingSpan(clause, controls);
			}
		}
		bool IsMatch(AVSDK.Writ176 writ, Feature feature)
		{
			if (feature.discretePOS != 0)
				return feature.not == false
				? writ.pos == feature.discretePOS
				: writ.pos != feature.discretePOS;


			bool slashes = feature.feature.StartsWith("/") && feature.feature.EndsWith("/") && (feature.feature.Length > 2);
			String token = slashes
				? feature.feature.Substring(1, feature.feature.Length - 2)
				: feature.feature;

			bool not = feature.not;

			if (slashes)
			{
				switch (feature.subtype)
				{
					case AVXAPI.SLASH_BitwisePOS:	foreach (UInt16 value in feature.featureMatchVector)
														if ((value & writ.pnwc) == value)
															return !not;
													return not;
					case AVXAPI.SLASH_Puncuation:	foreach (UInt16 value in feature.featureMatchVector)
														if ((value & writ.punc) == value)
															return !not;
													return not;
					case AVXAPI.SLASH_Boundaries:	foreach (UInt16 value in feature.featureMatchVector)
														if ((value & writ.trans) == value)
															return !not;
													return not;
					case AVXAPI.SLASH_RESERVED_80:
					case AVXAPI.SLASH_RESERVED_40:
					case AVXAPI.SLASH_RESERVED_20:
					case AVXAPI.SLASH_RESERVED_10:
					default:						return not;
				}
			}
			else
			{
				// incomplete!!!
				switch (feature.type & (AVXAPI.FIND_Token | AVXAPI.FIND_Lemma | AVXAPI.FIND_GlobalTest | AVXAPI.FIND_LANGUAGE_NUMERIC))
				{
					// TODO: add suffix support:
					/*
					* FIND_Suffix_MASK
					* FIND_Suffix_Exact
					* FIND_Suffix_Modern
					* FIND_Suffix_Either
					* FIND_Suffix_None
					*/
					case AVXAPI.FIND_Token:
						foreach (UInt16 value in feature.featureMatchVector)
							if (value == (writ.word & 0x3FFF))
							{
								//											Console::Out->WriteLine("Found: " + feature->feature);
								return !not;
							}
						return not;
					case AVXAPI.FIND_English:
						foreach (UInt16 value in feature.featureMatchVector)
							if (value == (writ.word & 0x3FFF))
								return !not;
						return not;

					case AVXAPI.FIND_Hebrew:
						if (AVXAPI.SELF.XVerse.GetBook(writ.verseIdx) <= 39)
						unsafe {
							int i = 0;
							for (var strongs = (UInt16*)writ.strongs; *strongs != 0; strongs++)
								foreach(UInt16 value in feature.featureMatchVector)
								{
									if (value == *strongs)
										return !not;
									if (++i == 4)
										return not;
								}
						}
						return not;
					case AVXAPI.FIND_Greek:
						if (AVXAPI.SELF.XVerse.GetBook(writ.verseIdx) >= 40)
						unsafe {
							int i = 0;
							for (var strongs = (UInt16*)writ.strongs; *strongs != 0; strongs++)
								foreach (UInt16 value in feature.featureMatchVector)
								{
									if (value == *strongs)
										return !not;
									if (++i == 3)
										return not;
								}
						}
						return not;
					case AVXAPI.FIND_Lemma:
						foreach (UInt16 value in feature.featureMatchVector)
							if (value == writ.lemma)
								return !not;
						return not;
					case AVXAPI.FIND_GlobalTest: // #diff or #same // TODO: determine which one

						/*case typeWordSame:*/
						{
							UInt16 key = (UInt16) (writ.word & 0x3FFF);
							var record = AVLexicon.GetLexRecord(key);
							if (record == null)
								return not;
							else
								return record.Modern == record.Display ? !not : not;
						}
				}
			}
			return false;
		}
		private bool IsMatch(AVSDK.Writ176 writ, QSearchFragment frag)
		{
			foreach (var spec in frag.specifications) {
				bool matchedAny = false;
				foreach (var features in spec.matchAny) {
					bool matchedAll = true;
					foreach(var feature in features.features) {
						matchedAll = this.IsMatch(writ, (Feature)feature);
						if (!matchedAll)
							break;
					}
					matchedAny = matchedAll;
				}
				if (matchedAny)
					return true;
			}
			return false;
		}
		private UInt32 SearchClauseQuoted_ScopedUsingSpan(IQuelleSearchClause clause, IQuelleSearchControls controls)
		{
			return 0;
		}
		private UInt32 SearchClauseQuoted_ScopedUsingVerse(IQuelleSearchClause clause, IQuelleSearchControls controls)
		{
			UInt32 matchCnt = 0;
			UInt64 found = 0;
			var verseIdx = 0;

			var prev = Writ176.InitializedWrit;
			var writ = Writ176.InitializedWrit;

			UInt32 start = AVMemMap.CURRENT;
			UInt32 end = AVMemMap.CURRENT;

			UInt32 cursor = AVMemMap.FIRST;

			for (Byte b = 1; b <= 66; b++)
			{
				var book = AVXAPI.SELF.Books[b - 1];
				var cidx = book.chapterIdx;
				var ccnt = book.chapterCnt;

				for (var c = cidx; c < (UInt32)(cidx + ccnt); c++)
				{
					var chapter = AVXAPI.SELF.Chapters[c];
					var until = chapter.writIdx + chapter.wordCnt - 1;
					UInt16 span;
					for (cursor = chapter.writIdx; cursor <= until; cursor += span)
					{
						start = cursor;
						if (AVXAPI.SELF.XWrit.GetRecord(cursor, ref writ))
						{
							span = AVXAPI.SELF.XVerse.GetWordCnt(writ.verseIdx);
							UInt64 required = 0;
							var matched = false;

							foreach (QSearchFragment fragment in clause.fragments) {
								try
								{
									var position = this.SearchSequentiallyInSpan(span, fragment);
									matched = (position != 0xFFFFFFFF);
									end = start + position;

									if (!matched)
										break;
								}
								catch
								{
									return 0;
								}
							}
							if (matched) {
								if (clause.polarity == '-')
									this.SubtractMatch(start, end);
								else if (clause.polarity == '+')
									this.AddMatch(clause.index, start, end);

								matchCnt++;
							}
						}
						else
						{
							break;
						}
					}
				}
			}
			return matchCnt;
		}
		private UInt32 SearchSequentiallyInSpan(UInt16 span, QSearchFragment frag)
		{
			var cursor = AVXAPI.SELF.XWrit.cursor;
			var writ = Writ176.InitializedWrit;

			for (UInt32 i = 0; i < span; i++)
			{
				AVXAPI.SELF.XWrit.GetRecordWithoutMovingCursor(cursor++, ref writ);
				if (this.IsMatch(writ, frag))
				{
					var existing = this.Tokens.ContainsKey(cursor) ? this.Tokens[cursor] : (UInt64)(0);
					this.Tokens[cursor] = existing | frag.bit;
					return ++i;
				}
			}
			return 0xFFFFFFFF;
		}
		private UInt32 SearchClauseUnquoted_ScopedUsingSpan(IQuelleSearchClause clause, IQuelleSearchControls controls)
		{
			UInt32 matchCnt = 0;
			UInt64 found = 0;
			var verseIdx = 0;

			UInt32 localspan = 0;
			var prev = Writ176.InitializedWrit;
			var writ = Writ176.InitializedWrit;


			for (Byte b = 1; b <= 66; b++)
			{
				var book = AVXAPI.SELF.Books[b - 1];
				var cidx = book.chapterIdx;
				var ccnt = book.chapterCnt;
				var chapter = AVXAPI.SELF.Chapters[cidx];
				var chapterLast = AVXAPI.SELF.Chapters[cidx + ccnt - 1];

				UInt32 cursor = chapter.writIdx;
				UInt32 until = cursor + chapterLast.writIdx + chapterLast.wordCnt - 1;

				UInt32 start = AVMemMap.CURRENT;
				var span = (UInt16) controls.span;

				for (bool ok = AVXAPI.SELF.XWrit.GetRecord(cursor, ref writ); ok && (cursor <= until); cursor = AVXAPI.SELF.XWrit.cursor)
				{
					UInt64 required = 0;
					Byte f = 0;
					foreach (QSearchFragment fragment in clause.fragments) {
						var bits = (UInt64) (0x1 << f++);
						required |= bits;
						bool matched = this.SearchUnorderedInSpan(span, fragment) != 0xFFFFFFFF;
						if (matched)
						{
							found |= bits;
							if (start == AVMemMap.CURRENT)
								start = cursor;
						}
					}
					if (found == required)
					{
						if (clause.polarity == '-')
							this.SubtractMatch(start, cursor);
						else if (clause.polarity == '+')
							this.AddMatch(clause.index, start, cursor);

						start = AVMemMap.CURRENT;
						found = 0;
						matchCnt++;
					}
					else if (found != 0)
					{
						ok = AVXAPI.SELF.XWrit.GetRecord(cursor + span, ref writ);
					}
					else
					{
						ok = AVXAPI.SELF.XWrit.GetRecord(AVMemMap.NEXT, ref writ);
					}
				}
			}
			return matchCnt;
		}
		private UInt32 SearchClauseUnquoted_ScopedUsingVerse(IQuelleSearchClause clause, IQuelleSearchControls controls)
		{
			UInt32 matchCnt = 0;
			UInt64 found = 0;
			var verseIdx = 0;

			Writ176 prev;
			Writ176 writ;
			UInt32 start = AVMemMap.CURRENT;
			UInt32 cursor = AVMemMap.FIRST;

			for (Byte b = 1; b <= 66; b++)
			{
				var book = AVXAPI.SELF.Books[b - 1];
				var cidx = book.chapterIdx;
				var ccnt = book.chapterCnt;

				for (var c = cidx; c < (UInt32)(cidx + ccnt); c++)
				{
					var chapter = AVXAPI.SELF.Chapters[c];
					var until = chapter.writIdx + chapter.wordCnt - 1;
					Byte span = 0;
					for (cursor = chapter.writIdx; cursor <= until; cursor += span)
					{
						writ = Writ176.InitializedWrit;
						if (AVXAPI.SELF.XWrit.GetRecord(cursor, ref writ))
						{
							span = AVXAPI.SELF.XVerse.GetWordCnt(writ.verseIdx);
							UInt64 required = 0;

							foreach (QSearchFragment fragment in clause.fragments) {
								var bit = (UInt64) (0x1 << (int)(fragment.bit - 1));
								required |= bit;
								bool matched = (this.SearchUnorderedInSpan(span, fragment) != 0xFFFFFFFF);
								if (matched)
								{
									found |= bit;
									if (start == AVMemMap.CURRENT)
										start = cursor;
								}
							}
							if (found == required)
							{
								if (clause.polarity == '-')
									this.SubtractMatch(start, cursor);
								else if (clause.polarity == '+')
									this.AddMatch(clause.index, start, cursor);

								start = AVMemMap.CURRENT;
								found = 0;
								matchCnt++;
							}
						}
						else
						{
							break;
						}
					}
				}
			}
			return matchCnt;
		}
		// These methods used to include book, and chapter
		// There is a missing loop that creates the moving span window
		//
		private UInt32 SearchUnorderedInSpan(UInt16 span, QSearchFragment frag)
		{
			UInt32 cursor = AVXAPI.SELF.XWrit.cursor;
			UInt32 last = cursor + span - 1;
			var writ = Writ176.InitializedWrit;

			for (UInt32 i = 0; cursor <= last; cursor++, i++)
			{
				AVXAPI.SELF.XWrit.GetRecordWithoutMovingCursor(cursor, ref writ);
				if (this.IsMatch(writ, frag))
				{
					if (this.Tokens.ContainsKey(cursor))
						this.Tokens[cursor] |= frag.bit;
					else
						this.Tokens[cursor] = frag.bit;
					return i;
				}
			}
			return 0xFFFFFFFF;
		}
    }
}
