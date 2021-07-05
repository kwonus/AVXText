using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVSDK
{
    public enum Punctuation
    {
        PUNCclause = 0xE0,
        PUNCexclamatory = 0x80,
        PUNCinterrogative = 0xC0,
        PUNCdeclarative = 0xE0,
        PUNCdash = 0xA0,
        PUNCsemicolon = 0x20,
        PUNCcomma = 0x40,
        PUNCcolon = 0x60,
        PUNCpossessive = 0x10,
        ENDparenthetical = 0x0C,
        MODEparenthetical = 0x04,
        MODEitalics = 0x02,
        MODEjesus = 0x01
    }
    public enum Transitions_Vi607
    {
        EndBit = 0x01, // (0b0001)
        VerseTransition = 0x03, // (0b0010)
        BeginingOfVerse = 0x02, // (0b0010)
        EndOfVerse = 0x03, // (0b0011)
        ChapterTransition = 0x07, // (0b0110)
        BeginingOfChapter = 0x06, // (0b0110)
        EndOfChapter = 0x07, // (0b0111)
        BookTransition = 0x0F, // (0b1110)
        BeginingOfBook = 0x0E, // (0b1110)
        EndOfBook = 0x0F  // (0b1111)
    }
    public enum Transitions
    {
        EndBit = 0x10, // (0b0001____)
        VerseTransition = 0x30, // (0b0010____)
        BeginingOfVerse = 0x20, // (0b0010____)
        EndOfVerse = 0x30, // (0b0011____)
        ChapterTransition = 0x70, // (0b0110____)
        BeginingOfChapter = 0x60, // (0b0110____)
        EndOfChapter = 0x70, // (0b0111____)
        BookTransition = 0xF0, // (0b1110____)
        BeginingOfBook = 0xE0, // (0b1110____)
        EndOfBook = 0xF0,  // (0b1111____)
        BeginningOfBible = 0xE8,
        EndOfBible = 0xF8
    }
    public enum PersonNumber
    {
        PersonBits = 0x3000,
        NumberBits = 0xC000,
        Indefinite = 0x0000,
        Person1st = 0x1000,
        Person2nd = 0x2000,
        Person3rd = 0x3000,
        Singular = 0x4000,
        Plural = 0x8000,
        ConstructWH = 0xC000
    }
    public enum WordCapitalization
    {
        EnglishWord = 0x3FFF,   // (This mask produces lookup key for word) 
        Cap1stLetter = 0x8000,   // (example: Lord) 
        CapAllLetters = 0x4000    // (example: LORD) 
    }
    public enum PronounGender
    {
        Neuter = 0x0001,
        Masculine = 0x0002,
        Nonfeminine = 0x0003,
        Feminine = 0x0004,
        Genitive = 0x0008,
        Unmarked = 0x0000    // new in July 2018
    }
    // REVIEW:
#if NEVER
    public enum PronounCase
    {
        Nominative = 0x0070,
        Oblique = 0x00B0,
        Reflexive = 0x00F0,
        Unmarked = 0x0000,
    }
    public enum PronounGenitive
    {
        Possessive = 0x0008,
        Adjective = 0x0A08
    }
#endif
    public static class PunctuationMarking
    {
        public static string PrePunc(UInt16 previous, UInt16 current)
        {
            bool prevParen = ((previous & (UInt16)Punctuation.MODEparenthetical) != (UInt16)0);
            bool thisParen = ((current  & (UInt16)Punctuation.MODEparenthetical) != (UInt16)0);

            return (thisParen && !prevParen) ? "(" : "";
        }
        public static string PostPunc(UInt16 current, bool s)
        {
            bool eparen  = ((current & (UInt16)Punctuation.ENDparenthetical)  == (UInt16)Punctuation.ENDparenthetical);
            bool posses  = ((current & (UInt16)Punctuation.PUNCpossessive)    == (UInt16)Punctuation.PUNCpossessive);
            bool exclaim = ((current & (UInt16)Punctuation.PUNCclause)        == (UInt16)Punctuation.PUNCexclamatory);
            bool declare = ((current & (UInt16)Punctuation.PUNCclause)        == (UInt16)Punctuation.PUNCdeclarative);
            bool dash    = ((current & (UInt16)Punctuation.PUNCclause)        == (UInt16)Punctuation.PUNCdash);
            bool semi    = ((current & (UInt16)Punctuation.PUNCclause)        == (UInt16)Punctuation.PUNCsemicolon);
            bool colon   = ((current & (UInt16)Punctuation.PUNCclause)        == (UInt16)Punctuation.PUNCcolon);
            bool comma   = ((current & (UInt16)Punctuation.PUNCclause)        == (UInt16)Punctuation.PUNCcomma);
            bool quest   = ((current & (UInt16)Punctuation.PUNCclause)        == (UInt16)Punctuation.PUNCinterrogative);

            String punc = posses ? !s ? "'s" : "'" : "";   // must be post processed by caller if root ends with S
            if (eparen)
                punc += ")";
            if (declare)
                punc += ".";
            else if (comma)
                punc += ",";
            else if (semi)
                punc += ";";
            else if (colon)
                punc += ":";
            else if (quest)
                punc += "?";
            else if (exclaim)
                punc += "!";
            else if (dash)
                punc += "--";


            return punc;
        }
    }
}
