using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVSDK
{
    public struct Chapter
    {
        public UInt32 writIndex;
        public UInt16 verseIndex;
        public UInt16 wordCount;

        internal Chapter(UInt32 writIdx, UInt16 verseIdx, UInt16 wordCnt)
        {
            this.writIndex = writIdx;
            this.verseIndex = verseIdx;
            this.wordCount = wordCnt;
        }
    }

    public class IXChapter
    {
        public Chapter[] chapters;
        private UInt16 maxBookSize;
        private Book[] books;
        private IXVerse ixverse;
        public bool okay;

        public UInt16 MaxBookSize
        {
            get
            {
                return maxBookSize;
            }
        }

        public IXChapter(string sdk, ILittleEndianReader reader, Book[] books, IXVerse ixverse)
        {

            this.maxBookSize = 0;
            this.books = books;
            this.ixverse = ixverse;

            okay = (books != null) && (ixverse != null);

            if (okay)
            {
                var list = new List<Chapter>();
                var quad = new byte[4];
                var path = System.IO.Path.Combine(sdk, "AV-Chapter.ix");
                var input = new System.IO.StreamReader(path);

                UInt32? writIndex;

                UInt16 currentWordCnt = 0;
                byte previousBook = 0;
                for (/**/;/**/;/**/)
                {
                    writIndex = reader.ReadUInt32(quad, input.BaseStream);
                    if (writIndex == null)
                    {
                        okay = (list.Count > 1000);
                        break;
                    }
                    var versIndex = reader.ReadUInt16(quad, input.BaseStream);  // verseIndex is wrong here !!!
                    var wordCount = reader.ReadUInt16(quad, input.BaseStream);

                    okay = (versIndex != null && wordCount != null);
                    if (okay)
                    {
                        list.Add(new AVSDK.Chapter(writIndex.Value, versIndex.Value, wordCount.Value));
                    }
                    else break;
                    currentWordCnt += wordCount.Value;

                    byte bk = this.ixverse.GetBook(versIndex.Value);
                    if (bk != previousBook)
                    {
                        if (currentWordCnt > this.maxBookSize)
                            this.maxBookSize = currentWordCnt;
                        currentWordCnt = 0;
                        previousBook = bk;
                    }
                }
                if (okay)
                {
                    this.chapters = list.ToArray();
                }
                else
                {
                    this.chapters = null;
                }
                input.Close();
            }
        }
        public UInt32 GetMaxBookSize()
        {
            return maxBookSize;
        }
        public UInt16 GetWordCnt(byte bookNum, byte chapterNum = 0) // chapterNum == 0 means get wordCnt for book (aka all chapters)
        {
            UInt16 wordCnt = 0;

            if (bookNum >= 1 && bookNum <= 66)
            {
                UInt16 b = (UInt16)(bookNum - 1);

                for (UInt16 i = books[b].chapterIdx; books[b].num == bookNum && i < chapters.Length; i++)
                {
                    if (chapterNum == 0)
                    {
                        wordCnt += chapters[i].wordCount;
                    }
                    else
                    {
                        byte chapter = ixverse.GetChapter(chapters[i].verseIndex);
                        if (chapter == chapterNum)
                        {
                            wordCnt = chapters[i].wordCount;
                            break;
                        }
                    }
                }
            }
            return wordCnt;
        }
    }
}
