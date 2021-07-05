using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVSDK
{
    public class IXVerse
    {
        public bool okay;
        protected UInt32[] verses; // Book|Chapter|Verse|WordCnt

        public IXVerse(string sdk, ILittleEndianReader reader)
        {
            var list = new List<UInt32>();
            var path = System.IO.Path.Combine(sdk, "AV-Verse.ix"); // TODO: AV-Verse.IX2 does not contain valid word counts;
            var input = new System.IO.StreamReader(path);

            byte[] quad = new byte[4];
            int cnt;
            for (cnt = reader.Read(quad, input.BaseStream); cnt == 4; cnt = reader.Read(quad, input.BaseStream))
            {
                UInt32 index = (UInt32)((quad[0] * 0x1000000) + (quad[1] * 0x10000) + (quad[2] * 0x100) + quad[3]);
                list.Add(index);
            }
            this.okay = (cnt == 0) && (list.Count == 31102);
            this.verses = list.ToArray();

            input.Close();
        }

        public byte GetBook(UInt16 index)
        {
            UInt32 entry = verses[index];
            UInt32 book = entry / 0x1000000;
            return (byte)book;
        }
        public byte GetChapter(UInt16 index)
        {
            UInt32 entry = verses[index];
            UInt32 chapter = (entry & 0xFF0000) / 0x10000;
            return (byte)chapter;
        }
        public byte GetVerse(UInt16 index)
        {
            UInt32 entry = verses[index];
            UInt32 verse = (entry & 0xFF00) / 0x100;
            return (byte)verse;
        }
        public byte GetWordCnt(UInt16 index)
        {
            if (index > 31101)
                return 0;
            
            UInt32 entry = verses[index];
            UInt32 cnt = entry & 0xFF;
            return (byte)cnt;
        }
        public bool GetEntry(UInt16 index, out byte book, out byte chapter, out byte verse, out byte wordCnt)
        {
            if (index < verses.Length)
            {
                UInt32 entry = verses[index];
                book    = (byte)(entry / 0x1000000);
                chapter = (byte)((entry & 0xFF0000) / 0x10000);
                verse   = (byte)((entry & 0xFF00) / 0x100);
                wordCnt = (byte)(entry & 0xFF);
                return true;
            }
            book    = 0;
            chapter = 0;
            verse   = 0;
            wordCnt = 0;
            return false;
        }
    }
}
