using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVSDK
{
    public interface ILittleEndianReader
    {
        Boolean FromUInt32(byte[] quad, int offset, UInt32 val);
        Boolean FromUInt16(byte[] pair, int offset, UInt16 val);

        UInt32? ToUInt32(byte[] quad);
        UInt32? ReadUInt32(byte[] quad, System.IO.Stream stream);
        UInt16? ToUInt16(byte[] pair);
        UInt16? ReadUInt16(byte[] pair, System.IO.Stream stream);
        byte? ReadByte(byte[] single, System.IO.Stream stream);
        int Read(byte[] all, System.IO.Stream stream);
        int Read(byte[] partial, int size, System.IO.Stream stream);
    }
    public class BibleIndexOnly: ILittleEndianReader
    {
        protected Boolean okay;

        public IXBook ixbook;
        public IXChapter ixchapter;
        public IXVerse ixverse;

        public BibleIndexOnly(string sdk)
        {
            this.ixbook = new IXBook(sdk, this);
            this.ixverse = new IXVerse(sdk, this);
            this.ixchapter = new IXChapter(sdk, this, this.ixbook.books, this.ixverse);

            this.okay = this.ixbook.okay && this.ixchapter.okay && this.ixverse.okay;
        }
        public byte? ReadByte(byte[] single, System.IO.Stream stream)
        {
            if ((single.Length >= 1) && (stream.Read(single, 0, 1) == 1))
            {
                return single[0];
            }
            return null;
        }
        public int Read(byte[] all, System.IO.Stream stream)
        {
            int len = all.Length;
            int cnt = 0;
            if (len >= 1)
            {
                cnt = stream.Read(all, 0, len);
            }
            return cnt;
        }
        public int Read(byte[] all, int size, System.IO.Stream stream)
        {
            int len = (all.Length >= size) ? size : 0;
            int cnt = 0;
            if (len >= 1)
            {
                cnt = stream.Read(all, 0, len);
            }
            return cnt;
        }
        // AV-SDK uses Big-Endian byte order on incoming hex-streams. just like every high-level language
        public UInt32? ToUInt32(byte[] quad)
        {
            if (quad.Length >= 4)
            {
                return (UInt32) BitConverter.ToInt32(quad, 0);
            }
            return null;
        }
        public Boolean FromUInt32(byte[] quad, int offset, UInt32 val)
        {
            if (quad.Length - offset >= 4)
            {
                var bytes = BitConverter.GetBytes(val);
                for (int i = 0; i < 4; i++)
                    quad[offset+i] = bytes[i];
                return true;
            }
            return false;
        }

        // AV-SDK uses Little-Endian byte order to store data
        public UInt32? ReadUInt32(byte[] quad, System.IO.Stream stream)
        {
            if ( (quad.Length >= 4) && (stream.Read(quad, 0, 4) == 4) )
            {
                return ToUInt32(quad);
            }
            return null;
        }
        // AV-SDK uses Big-Endian byte order on incoming hex-streams. just like every high-level language
        public UInt16? ToUInt16(byte[] pair)
        {
            if (pair.Length >= 2)
            {
                return (UInt16)BitConverter.ToInt16(pair, 0);
            }
            return null;
        }
        public Boolean FromUInt16(byte[] pair, int offset, UInt16 val)
        {
            if (pair.Length >= 2)
            {
                var bytes = BitConverter.GetBytes(val);
                pair[offset  ] = bytes[0];
                pair[offset+1] = bytes[1];
                return true;
            }
            return false;
        }
        // AV-SDK uses Little-Endian byte order to store data
        public UInt16? ReadUInt16(byte[] pair, System.IO.Stream stream)
        {
            if ( (pair.Length >= 2) && (stream.Read(pair, 0, 2) == 2) )
            {
                return ToUInt16(pair);
            }
            return null;
        }
    }
    public class Bible : BibleIndexOnly
    {
        public IXFile writ;

        public Bible(string sdk): base(sdk)
        {
            if (this.okay)
            {
                this.writ = new IXFile(sdk, this);
                this.okay = this.writ.okay;
            }
            else
            {
                this.writ = null;
            }
        }
    }
    
    public class IXFile
    {
        private byte[] data;
        public UInt64[] search;
        private System.IO.StreamReader file;
        private Bible bible;
        
        public bool okay
        {
            get
            {
                return data != null;
            }
        }

        public IXFile(string sdk, Bible bible)
        {
            this.bible = bible;
            if (bible != null && bible.ixchapter != null && bible.ixchapter.MaxBookSize > 0)
            {
                this.search = new UInt64[bible.ixchapter.MaxBookSize];
                this.data = new byte[bible.ixchapter.MaxBookSize * 16];

                var path = System.IO.Path.Combine(sdk, "AV-Writ.dx");
#if (Windows || TRUE)   //  Windows locks the file here // evidentally, two distinct open file handles start here
                var stream = new System.IO.StreamReader(path);
                var mem = new System.IO.MemoryStream();
                byte[] buffer = new byte[4 * 1024];
                for (int len = stream.BaseStream.Read(buffer, 0, buffer.Length); len > 0; len = stream.BaseStream.Read(buffer, 0, buffer.Length))
                    mem.Write(buffer, 0, len);
                this.file = new System.IO.StreamReader(mem);
                stream.Close();
#else
                this.file = new System.IO.StreamReader(path);
#endif
            }
            else
            {
                this.search = null;
                this.data = null;
                this.file = null;
            }
        }
        public byte[] ReadData(byte book, /* byte chapter, */ out UInt16 size)
        {
            size = this.bible.ixchapter.GetWordCnt(book);
//          UInt16 chapterIndex = (UInt16) (bible.ixbook.books[book-1].chapterIdx + chapter-1);
//          UInt32 index = bible.ixchapter.chapters[chapterIndex].writIndex;
            int cnt = bible.Read(data, size, file.BaseStream);

            return (cnt == size) ? data : null;
        }
        internal UInt16 ToUInt16(byte[] array, UInt32 offset) // little-endian, w/o bounds checks for speed
        {
            UInt16 result = (UInt16)(array[offset] * 0x100);
            result += (UInt16)(array[offset+1]);

            return result;
        }
        public UInt16[] GetStrongs(UInt16 record) // 0 <= record < dataCurrentSize
        {
            UInt32 pointer = (UInt32) (record * 16);

            var strongs = new UInt16[4];
            for (int i = 0; i < 4; i++)
                strongs[i] = this.ToUInt16(data, pointer);
            return strongs;
        }
        public UInt16 GetVerseIndex(UInt16 record) // 0 <= record < dataCurrentSize
        {
            UInt32 pointer = (UInt32)(record * 16);
            return this.ToUInt16(data, pointer+8);
        }
        public UInt16 GetWordKey(UInt16 record) // 0 <= record < dataCurrentSize
        {
            UInt32 pointer = (UInt32)(record * 16);
            return (UInt16) (this.ToUInt16(data, pointer + 10) & 0x3FF);
        }
        public UInt16 GetCapitolization(UInt16 record) // 0 <= record < dataCurrentSize
        {
            UInt32 pointer = (UInt32)(record * 16);
            return (UInt16)(this.ToUInt16(data, pointer + 10) & 0xC000);
        }
        public UInt16 GetTransition(UInt16 record) // 0 <= record < dataCurrentSize
        {
            UInt32 pointer = (UInt32)(record * 16);
            return (byte) (data[pointer + 13] & 0xF);
        }
        public UInt16 GetPos(UInt16 record) // 0 <= record < dataCurrentSize
        {
            UInt32 pointer = (UInt32)(record * 16);
            return this.ToUInt16(data, pointer + 14);
        }
        public void Release()
        {
            if (file != null)
            {
                file.Close();
                file = null;
            }
        }
    }
}
