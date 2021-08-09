using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVSDK
{
    public class IXVerse
    {
        public bool okay;
        public UInt32[] verses; // Book|Chapter|Verse|WordCnt

        public IXVerse(string sdk)
        {
            var list = new UInt32[0x797D+1];

            var data = AVMemMap.Fetch("AV-Verse.ix", sdk);

            var ok = (data != null) && File.Exists(data);

            if (ok)
            {
                var input = new System.IO.StreamReader(data);
                var binary = new System.IO.BinaryReader(input.BaseStream);

                for (int i = 0; i < list.Length; i++)
                {
                    byte[] quad = binary.ReadBytes(4);
                    list[i] = (UInt32)((quad[0] * 0x1000000) + (quad[1] * 0x10000) + (quad[2] * 0x100) + quad[3]);
                }
                this.verses = list;

                binary.Close();
                input.Close();
            }
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
