using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVSDK
{
    public class Chapter
    {
        public UInt32 writIdx;
        public UInt16 verseIdx;
        public UInt16 wordCnt;

        internal Chapter(UInt32 writIdx, UInt16 verseIdx, UInt16 wordCnt)
        {
            this.writIdx = writIdx;
            this.verseIdx = verseIdx;
            this.wordCnt = wordCnt;
        }
    }
    public class IXChapter
    {
        public Chapter[] chapters { get; private set; }
        public IXChapter(string sdk)
        {
            var list = new Chapter[0x4A4+1];
            var quad = new byte[4];
            var path = System.IO.Path.Combine(sdk, "AV-Chapter.ix");
            var input = new System.IO.StreamReader(path);
            var binary = new System.IO.BinaryReader(input.BaseStream);
            for (int i = 0; i < list.Length; i++)
            {
                var writIndex = binary.ReadUInt32();
                var versIndex = binary.ReadUInt16();
                var wordCount = binary.ReadUInt16();

                list[i] = new Chapter(writIndex, versIndex, wordCount);
            }
            this.chapters = list;

            binary.Close();
            input.Close();
        }
    }
}
