using System;
using System.Collections.Generic;
using System.Linq;

using QuelleHMI;
using QuelleHMI.Tokens;
using AVSDK;
using QuelleHMI.Definitions;
using QuelleHMI.Actions;

namespace AVText
{
    class BookChapterVerse
    {
		public HashSet<UInt16> Verses { get; private set; }
		public HashSet<UInt32> Tokens { get; private set; }
		public BookChapterVerse()
		{
			this.Verses = new HashSet<UInt16>();    // this could be managed by Maganimity
			this.Tokens = new HashSet<UInt32>();    // this could be managed by Maganimity
		}

		public bool AddVerse(UInt16 vidx)
        {
			if (!this.Verses.Contains(vidx))
			{
				this.Verses.Add(vidx);
				return true;
			}
			return false;
		}
		public bool SubtractVerse(UInt16 vidx)
		{
			if (this.Verses.Contains(vidx))
			{
				this.Verses.Remove(vidx);
				return true;
			}
			return false;
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
		private bool IsMatch(UInt32 widx, AVSDK.Writ176 writ, QSearchFragment frag)
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
				{
					this.Tokens.Add(widx);
					return true;
				}
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
							UInt16 reducableSpan = span;
							var matched = false;
							var first = true;

							foreach (QSearchFragment fragment in clause.fragments)
							{
								if (reducableSpan == 0)
                                {
									matched = false;
									break;
                                }
								bool adjacency = (fragment.adjacency == 1);
								try
								{
									UInt32 position;
									if (first)
									{
										position = this.SearchSequentiallyInSpan(span, fragment);
										matched = (position != 0xFFFFFFFF);
										first = false;
									}
									else
									{
										UInt16 localSpan = adjacency ? (UInt16)1 : reducableSpan;
										position = this.SearchSequentiallyInSpan(localSpan, fragment);
										matched = (position != 0xFFFFFFFF);
									}
									end = start + position;

									if (matched)
                                    {
										if (reducableSpan > position)
											reducableSpan -= (UInt16)position;
										else
											reducableSpan = 0;
									}
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
								{
									this.SubtractVerse(writ.verseIdx);
									if ( (start != end) && AVXAPI.SELF.XWrit.GetRecord(start, ref prev) && (prev.verseIdx != writ.verseIdx))
										this.SubtractVerse(prev.verseIdx);
								}
								else if (clause.polarity == '+')
								{
									this.AddVerse(writ.verseIdx);
									if ((start != end) && AVXAPI.SELF.XWrit.GetRecord(start, ref prev) && (prev.verseIdx != writ.verseIdx))
										this.AddVerse(prev.verseIdx);
								}
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
				AVXAPI.SELF.XWrit.GetRecord(cursor, ref writ);
				if (this.IsMatch(cursor, writ, frag))
				{
					AVXAPI.SELF.XWrit.GetRecord(cursor+1, ref writ);
					return i+1;
				}
				cursor++;
			}
			return 0xFFFFFFFF;
		}
		private UInt32 SearchClauseUnquoted_ScopedUsingSpan(IQuelleSearchClause clause, IQuelleSearchControls controls)
		{
			UInt32 matchCnt = 0;
			UInt64 required = 0;

			foreach (QSearchFragment fragment in clause.fragments)
			{
				required |= fragment.bit;
			}

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
					var found = this.SearchUnorderedInSpanCrossingVerseBoundaries(span, clause);

					if ((found.bits & required) == required && found.indexes != null)
					{
						if (clause.polarity == '-')
						{
							foreach (var vidx in found.indexes)
							{
								this.SubtractVerse(vidx);
								matchCnt++;
							}
						}
						else if (clause.polarity == '+')
						{
							foreach (var vidx in found.indexes)
							{
								this.AddVerse(vidx);
								matchCnt++;
							}
						}
						start = AVMemMap.CURRENT;
					}
				}
			}
			return matchCnt;
		}
		private UInt32 SearchClauseUnquoted_ScopedUsingVerse(IQuelleSearchClause clause, IQuelleSearchControls controls)
		{
			UInt32 matchCnt = 0;
			UInt64 required = 0;
			var verseIdx = 0;

			foreach (QSearchFragment fragment in clause.fragments)
			{
				required |= fragment.bit;
			}
			Writ176 writ = Writ176.InitializedWrit;
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
						if (AVXAPI.SELF.XWrit.GetRecord(cursor, ref writ))
						{
							span = AVXAPI.SELF.XVerse.GetWordCnt(writ.verseIdx);
							if (span == 0)
								break;

							var found = this.SearchUnorderedInSpan(span, clause);

							if ((found.bits & required) == required)
							{
								if (clause.polarity == '-')
								{
									this.SubtractVerse(found.vidx);
								}
								else if (clause.polarity == '+')
								{
									this.AddVerse(found.vidx);
								}
								start = AVMemMap.CURRENT;
								matchCnt++;
							}
						}
						else break;
					}
				}
			}
			return matchCnt;
		}
		private (UInt64 bits, UInt16 vidx) SearchUnorderedInSpan(UInt16 span, IQuelleSearchClause clause)
		{
			UInt64 bits = 0;
			UInt32 cursor = AVXAPI.SELF.XWrit.cursor;
			UInt32 last = cursor + span - 1;
			var writ = Writ176.InitializedWrit;

			for (UInt32 i = 0; cursor <= last; cursor++, i++)
			{
				AVXAPI.SELF.XWrit.GetRecordWithoutMovingCursor(cursor, ref writ);
				foreach (var frag in clause.fragments)
				{
					if (this.IsMatch(cursor, writ, (QSearchFragment)frag))
					{
						bits |= frag.bit;
					}
				}
			}
			return (bits, writ.verseIdx);
		}
		private (UInt64 bits, HashSet<UInt16> indexes) SearchUnorderedInSpanCrossingVerseBoundaries(UInt16 span, IQuelleSearchClause clause)
		{
			UInt64 bits = 0;
			UInt32 cursor = AVXAPI.SELF.XWrit.cursor;
			UInt32 last = cursor + span - 1;
			var writ = Writ176.InitializedWrit;
			HashSet<UInt16> indexes = null;

			for (UInt32 i = 0; cursor <= last; cursor++, i++)
			{
				AVXAPI.SELF.XWrit.GetRecordWithoutMovingCursor(cursor, ref writ);
				foreach (var frag in clause.fragments)
				{
					if (this.IsMatch(cursor, writ, (QSearchFragment)frag))
					{
						bits |= frag.bit;
						if (indexes == null)
							indexes = new HashSet<UInt16>();
						if (!indexes.Contains(writ.verseIdx))
							indexes.Add(writ.verseIdx);
					}
				}
			}
			return (bits, indexes);
		}
	}
}
