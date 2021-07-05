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
        public string name;
        public string[] alt;
        public UInt16 num;
        public UInt16 chapterIdx;
        public byte chapterCnt;
    }

    public class IXBook
    {
        public bool okay;
        public Dictionary<string, Book> bookByName;
        public Book[] books;

        public IXBook(string sdk, ILittleEndianReader reader)
        {
            books = new Book[66];
            okay = false;

            bookByName = new Dictionary<string, Book>();

            var path = System.IO.Path.Combine(sdk, "AV-Book.ix");
            var input = new System.IO.StreamReader(path);

            byte[] obj = new byte[32];
            UInt16 b = 0;
            for (byte n = 1; n <= 66; n++, b++)
            {
                books[b] = new AVSDK.Book();

                var index = reader.ReadUInt16(obj, input.BaseStream);
                okay = (index != null);
                if (okay)
                    books[b].chapterIdx = index.Value;
                else break;

                var bk = reader.ReadByte(obj, input.BaseStream);
                okay = (bk != null) && (n == bk.Value);
                if (okay)
                    books[b].num = n;
                else break;

                var ch = reader.ReadByte(obj, input.BaseStream);
                okay = (ch != null) && (ch.Value >= 1);
                if (okay)
                    books[b].chapterCnt = ch.Value;
                else break;

                okay = (reader.Read(obj, input.BaseStream) == 32);
                if (okay)
                {
                    int slash = 0;
                    string str = "";
                    var alt = new List<string>();
                    for (int i = 0; i <= 32 && obj[i] != 0; i++)
                    {
                        if (obj[i] == '/')
                        {
                            if (++slash == 1)
                                books[b].name = str;
                            else
                                alt.Add(str);
                            str = "";
                        }
                        else
                        {
                            str += (char)obj[i];
                        }
                    }
                    books[b].alt = alt.ToArray();
                    bookByName.Add(books[b].name.ToLower(), books[b]);
                }
                else break;
            }
            input.Close();
        }
    }
}
