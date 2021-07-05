using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVSDK
{
    public struct Writ176
    {
        public UInt64 strongs;
        public UInt16 verseIdx;
        public UInt16 word;
        public byte   punc;
        public byte   trans;
        public UInt16 pnwc;
        public UInt32 pos;
        public UInt16 lemma;
    }
    public struct Writ128
    {
        public UInt64 strongs;
        public UInt16 verseIdx;
        public UInt16 word;
        public byte   punc;
        public byte   trans;
        public UInt16 pnwc;
    }
    public struct Writ32
    {
        public UInt16 word;
        public byte   punc;
        public byte   pnwc;
    }
}
