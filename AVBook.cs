using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVSDK
{
 //   public struct BookAbbreviations
 //   {
 //       public string abbr;
 //       public byte minLen;
 //   }
    public struct Book
    {
        public byte num;
        public byte chapterCnt;
        public UInt16 chapterIdx;
        public string name;
        public string[] abbreviations;
    }

    public class IXBook
    {
        public bool okay;
        public Dictionary<string, Book> bookByName;
        public Book[] books;

        private char[] comma = new char[] { ',' };

        public IXBook(string sdk)
        {
            books = new Book[66];
            okay = false;

            bookByName = new Dictionary<string, Book>();

            var path = System.IO.Path.Combine(sdk, "AV-Book.ix");
            var input = new System.IO.StreamReader(path);
            var binary = new System.IO.BinaryReader(input.BaseStream);

            byte[] obj = new byte[32];
            UInt16 b = 0;
            for (byte n = 1; n <= 66; n++, b++)
            {
                books[b] = new AVSDK.Book();
                books[b].num = binary.ReadByte();
                books[b].chapterCnt = binary.ReadByte();
                books[b].chapterIdx = binary.ReadUInt16();
                var bytes = binary.ReadChars(16);
                books[b].name = new string(bytes);
                var len = books[b].name.IndexOf('\0');
                if (len > 0)
                    books[b].name = books[b].name.Substring(0, len);
                bytes = binary.ReadChars(12);
                var abbreviation = new string(bytes);
                len = abbreviation.IndexOf('\0');
                if (len > 0)
                    abbreviation = abbreviation.Substring(0, len);
                books[b].abbreviations = abbreviation.Split(comma, StringSplitOptions.RemoveEmptyEntries);

                bookByName.Add(books[b].name.ToLower(), books[b]);
            }
            binary.Close();
            input.Close();
        }
        public Book? GetBookByNum(byte num)
        {
            if (num < 1 || num > 66)
                return null;
            return this.books[num-1];
        }
        public Book? GetBookByIdx(byte idx)
        {
            if (idx < 0 || idx >= 66)
                return null;
            return this.books[idx];
        }
        public Book? GetBookByName(string name)
        {
            if (name == null)
                return null;
            return this.bookByName.ContainsKey(name) ? this.bookByName[name] : (Book?)null;
        }
    }
}
