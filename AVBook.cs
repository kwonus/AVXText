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

        public IXBook(string sdk, ILittleEndianReader reader)
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
                var book = books[b];
                book.num = binary.ReadByte();
                book.chapterCnt = binary.ReadByte();
                book.chapterIdx = binary.ReadUInt16();
                var bytes = binary.ReadChars(16);
                book.name = new string(bytes);
                bytes = binary.ReadChars(12);
                book.abbreviations = new string(bytes).Split(comma, StringSplitOptions.RemoveEmptyEntries);

                bookByName.Add(book.name.ToLower(), book);
            }
            binary.Close();
            input.Close();
        }
    }
}
