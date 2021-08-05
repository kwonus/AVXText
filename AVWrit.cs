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
        public byte punc;
        public byte trans;
        public UInt16 pnwc;
        public UInt32 pos;
        public UInt16 lemma;

        public static Writ176 InitializedWrit
        {
            get
            {
                Writ176 writ = new Writ176();
                writ.strongs = 0;
                writ.verseIdx = 0;
                writ.word = 0;
                writ.pnwc = 0;
                writ.trans = 0;
                writ.pnwc = 0;
                writ.pos = 0;
                writ.lemma = 0;

                return writ;
            }
        }
    }
    public struct Writ128
    {
        public UInt64 strongs;
        public UInt16 verseIdx;
        public UInt16 word;
        public byte punc;
        public byte trans;
        public UInt16 pnwc;

        public static Writ128 InitializedWrit128
        {
            get
            {
                Writ128 writ = new Writ128();
                writ.strongs = 0;
                writ.verseIdx = 0;
                writ.word = 0;
                writ.pnwc = 0;
                writ.trans = 0;
                writ.pnwc = 0;

                return writ;
            }
        }
    }
    public struct Writ32
    {
        public UInt16 word;
        public byte punc;
        public byte pnwc;

        public static Writ32 InitializedWrit128
        {
            get
            {
                Writ32 writ = new Writ32();
                writ.word = 0;
                writ.pnwc = 0;
                writ.pnwc = 0;

                return writ;
            }
        }
    }
}