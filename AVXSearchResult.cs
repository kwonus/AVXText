using System;
using System.Collections.Generic;
using System.Collections;
using QuelleHMI;
using QuelleHMI.Tokens;
using AVSDK;

namespace AVText
{
    class AVXSearchResult : AbstractQuelleSearchResult
    {
        private Dictionary<Byte, Dictionary<Byte, UInt32>> results;
        public AVXSearchResult(Dictionary<Byte, Dictionary<Byte, UInt32>> results, Char polarity)
        {
            this.positive = (polarity == '+');
            this.results = results;
        }
        public readonly bool positive;
        // We used to add/subtract whole bible at a time; new interface (to constrain RAM usage is a chapter at a time
        public Boolean Subtract(Dictionary<Byte, Dictionary<Byte, UInt32>> bibleMatches, Byte b, Byte c, Dictionary<Byte, UInt64> versesMatches)
        {
            if (b < 1 || b > 66 || c < 1)
                return false;
            var bk = AVXAPI.SELF.XBook.GetBookByNum(b).Value;
            if (bk.chapterCnt > c)
                return false;

            var book = bibleMatches.ContainsKey(b) ? bibleMatches[b] : null;
            if (book == null)
            {
                book = new Dictionary<Byte, UInt32>();
                bibleMatches[b] = book;
            }
            UInt32 wordIdx = 0;
            if (!book.ContainsKey(c))
            {
                var chap = bk.chapterIdx;
                Chapter chapter = AVXAPI.SELF.Chapters[chap++];
                wordIdx = chapter.verseIdx;
                for (Byte ch = 2; ch <= c; ch++)
                {
                    wordIdx += chapter.wordCnt;
                    chapter = AVXAPI.SELF.Chapters[chap++];
                }
                book[c] = wordIdx;
            }
            else wordIdx = book[c];

            return true;
        }
        // We used to add/subtract whole bible at a time; new interface is per chapter (to constrain RAM usage is a chapter at a time
        Boolean Add(Dictionary<Byte, Dictionary<Byte, UInt32>> bibleMatches, Byte b, Byte c, Dictionary<Byte, UInt64> versesMatches)
        {
            if (b < 1 || b > 66 || c < 1)
                return false;
            var bk = AVXAPI.SELF.XBook.GetBookByNum(b).Value;
            if (bk.chapterCnt > c)
                return false;

            var book = bibleMatches.ContainsKey(b) ? bibleMatches[b] : null;
            if (book == null)
            {
                book = new Dictionary<Byte, UInt32>();
                bibleMatches[b] = book;
            }
            UInt32 wordIdx = 0;
            if (!book.ContainsKey(c))
            {
                var chap = bk.chapterIdx;
                var chapter = AVXAPI.SELF.Chapters[chap++];
                wordIdx = chapter.writIdx;
                for (Byte ch = 2; ch <= c; ch++)
                {
                    wordIdx += chapter.wordCnt;
                    chapter = AVXAPI.SELF.Chapters[chap++];
                }
                book[c] = wordIdx;
            }
            else wordIdx = book[c];

            return true;
        }
    }
}
